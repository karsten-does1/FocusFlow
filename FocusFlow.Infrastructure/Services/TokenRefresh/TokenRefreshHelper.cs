using System;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Enums;
using FocusFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusFlow.Infrastructure.Services.TokenRefresh
{
    public static class TokenRefreshHelper
    {
        public static async Task<bool> EnsureValidTokenAsync(
            Guid accountId,
            EmailProvider provider,
            FocusFlowDbContext db,
            GmailTokenRefreshService? gmailRefreshService,
            OutlookTokenRefreshService? outlookRefreshService,
            ILogger logger,
            CancellationToken ct = default)
        {
            var account = await db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == accountId, ct);

            if (account == null)
            {
                logger.LogWarning("Account {AccountId} not found", accountId);
                return false;
            }

            if (string.IsNullOrWhiteSpace(account.RefreshToken))
            {
                logger.LogWarning("No refresh token available for account {AccountId}", accountId);
                return false;
            }

            var isExpired = DateTime.UtcNow >= account.AccessTokenExpiresUtc;
            var shouldRefresh = (account.AccessTokenExpiresUtc - DateTime.UtcNow) <= TimeSpan.FromMinutes(15);

            if (!isExpired && !shouldRefresh)
                return true;

            ITokenRefreshService? refreshService = provider switch
            {
                EmailProvider.Gmail => gmailRefreshService,
                EmailProvider.Outlook => outlookRefreshService,
                _ => null
            };

            if (refreshService == null)
            {
                logger.LogWarning("No refresh service available for provider {Provider}", provider);
                return false;
            }

            logger.LogInformation("Refreshing token for account {AccountId} (expired: {IsExpired})", accountId, isExpired);

            var result = await refreshService.RefreshTokenAsync(accountId, ct);

            if (!result.Success)
            {
                logger.LogError("Failed to refresh token for account {AccountId}: {Error}", accountId, result.ErrorMessage);
                return false;
            }

            logger.LogInformation("Successfully refreshed token for account {AccountId}", accountId);
            return true;
        }

        public static async Task<bool> ForceRefreshAsync(
            Guid accountId,
            EmailProvider provider,
            GmailTokenRefreshService? gmailRefreshService,
            OutlookTokenRefreshService? outlookRefreshService,
            ILogger logger,
            CancellationToken ct = default)
        {
            ITokenRefreshService? refreshService = provider switch
            {
                EmailProvider.Gmail => gmailRefreshService,
                EmailProvider.Outlook => outlookRefreshService,
                _ => null
            };

            if (refreshService == null)
            {
                logger.LogWarning("No refresh service available for provider {Provider}", provider);
                return false;
            }

            logger.LogInformation("Force-refreshing token for account {AccountId} ({Provider})", accountId, provider);

            var result = await refreshService.RefreshTokenAsync(accountId, ct);

            if (!result.Success)
            {
                logger.LogError("Force refresh failed for account {AccountId}: {Error}", accountId, result.ErrorMessage);
                return false;
            }

            logger.LogInformation("Force refresh succeeded for account {AccountId}", accountId);
            return true;
        }
    }
}
