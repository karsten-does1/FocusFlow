using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Enums;
using FocusFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FocusFlow.Infrastructure.Services.TokenRefresh
{
    public sealed class TokenRefreshBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenRefreshBackgroundService> _logger;

        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _refreshThreshold = TimeSpan.FromMinutes(15);

        public TokenRefreshBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TokenRefreshBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token refresh background service started");

            await SafeRunOnceAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await SafeRunOnceAsync(stoppingToken);
            }

            _logger.LogInformation("Token refresh background service stopped");
        }

        private async Task SafeRunOnceAsync(CancellationToken ct)
        {
            try
            {
                await RefreshTokensIfNeededAsync(ct);
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token refresh background service loop");
            }
        }

        private sealed record RunItem(
            EmailProvider Provider,
            Guid AccountId,
            string Email,
            string Reason);

        private sealed class RunStats
        {
            public int Refreshed { get; set; }
            public int Failed { get; set; }
            public int Skipped { get; set; }

            public List<RunItem> FailedAccounts { get; } = new();
            public List<RunItem> SkippedAccounts { get; } = new();
            public List<RunItem> RefreshedAccounts { get; } = new();
        }

        private async Task RefreshTokensIfNeededAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FocusFlowDbContext>();

            var dataSource = db.Database.GetDbConnection().DataSource;
            _logger.LogWarning("SQLite DataSource = {DataSource}", dataSource);

            var nowUtc = DateTime.UtcNow;
            var refreshBeforeUtc = nowUtc.Add(_refreshThreshold);

            var accountsNeedingRefresh = await db.EmailAccounts
                .AsNoTracking()
                .Where(a => !string.IsNullOrWhiteSpace(a.RefreshToken))
                .Where(a => a.AccessTokenExpiresUtc <= refreshBeforeUtc)
                .OrderBy(a => a.AccessTokenExpiresUtc)
                .ToListAsync(ct);

            if (accountsNeedingRefresh.Count == 0)
            {
                _logger.LogInformation(
                    "No accounts need token refresh (nowUtc={NowUtc:o}, refreshBeforeUtc={RefreshBeforeUtc:o})",
                    nowUtc, refreshBeforeUtc);
                return;
            }

            _logger.LogInformation(
                "Found {Count} accounts that need token refresh (nowUtc={NowUtc:o}, refreshBeforeUtc={RefreshBeforeUtc:o})",
                accountsNeedingRefresh.Count, nowUtc, refreshBeforeUtc);

            foreach (var a in accountsNeedingRefresh)
            {
                _logger.LogInformation(
                    "Token refresh candidate provider={Provider} accountId={AccountId} email={Email} expiresUtc={ExpiresUtc:o}",
                    a.Provider, a.Id, a.EmailAddress, a.AccessTokenExpiresUtc);
            }

            var stats = new RunStats();

            foreach (var account in accountsNeedingRefresh)
            {
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(account.RefreshToken))
                {
                    stats.Skipped++;
                    stats.SkippedAccounts.Add(new RunItem(
                        account.Provider, account.Id, account.EmailAddress, "Missing RefreshToken"));
                    _logger.LogWarning(
                        "Token refresh SKIPPED provider={Provider} accountId={AccountId} email={Email} reason={Reason}",
                        account.Provider, account.Id, account.EmailAddress, "Missing RefreshToken");
                    continue;
                }

                try
                {
                    ITokenRefreshService refreshService = account.Provider switch
                    {
                        EmailProvider.Gmail => scope.ServiceProvider.GetRequiredService<GmailTokenRefreshService>(),
                        EmailProvider.Outlook => scope.ServiceProvider.GetRequiredService<OutlookTokenRefreshService>(),
                        _ => throw new NotSupportedException($"No refresh service available for provider {account.Provider}")
                    };

                    var result = await refreshService.RefreshTokenAsync(account.Id, ct);

                    if (result.Success)
                    {
                        stats.Refreshed++;
                        stats.RefreshedAccounts.Add(new RunItem(
                            account.Provider, account.Id, account.EmailAddress, "OK"));

                        _logger.LogInformation(
                            "Token refresh SUCCESS provider={Provider} accountId={AccountId} email={Email}",
                            account.Provider, account.Id, account.EmailAddress);
                    }
                    else
                    {
                        stats.Failed++;
                        var reason = string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Unknown error" : result.ErrorMessage;

                        stats.FailedAccounts.Add(new RunItem(
                            account.Provider, account.Id, account.EmailAddress, reason));

                        _logger.LogWarning(
                            "Token refresh FAILED provider={Provider} accountId={AccountId} email={Email} error={Error}",
                            account.Provider, account.Id, account.EmailAddress, reason);
                    }
                }
                catch (NotSupportedException nse)
                {
                    stats.Skipped++;
                    var reason = nse.Message;

                    stats.SkippedAccounts.Add(new RunItem(
                        account.Provider, account.Id, account.EmailAddress, reason));

                    _logger.LogWarning(nse,
                        "Token refresh SKIPPED provider={Provider} accountId={AccountId} email={Email} reason={Reason}",
                        account.Provider, account.Id, account.EmailAddress, reason);
                }
                catch (Exception ex)
                {
                    stats.Failed++;
                    var reason = ex.Message;

                    stats.FailedAccounts.Add(new RunItem(
                        account.Provider, account.Id, account.EmailAddress, reason));

                    _logger.LogError(ex,
                        "Token refresh ERROR provider={Provider} accountId={AccountId} email={Email}",
                        account.Provider, account.Id, account.EmailAddress);
                }
            }

            _logger.LogInformation(
                "Token refresh run done: refreshed={Refreshed} skipped={Skipped} failed={Failed}",
                stats.Refreshed, stats.Skipped, stats.Failed);

            if (stats.FailedAccounts.Count > 0)
            {
                foreach (var f in stats.FailedAccounts)
                {
                    _logger.LogWarning(
                        "Token refresh FAILED account: provider={Provider} accountId={AccountId} email={Email} reason={Reason}",
                        f.Provider, f.AccountId, f.Email, f.Reason);
                }
            }

            if (stats.SkippedAccounts.Count > 0)
            {
                foreach (var s in stats.SkippedAccounts)
                {
                    _logger.LogWarning(
                        "Token refresh SKIPPED account: provider={Provider} accountId={AccountId} email={Email} reason={Reason}",
                        s.Provider, s.AccountId, s.Email, s.Reason);
                }
            }
        }
    }
}
