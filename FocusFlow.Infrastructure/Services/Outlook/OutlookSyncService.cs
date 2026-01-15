using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Core.Domain.Enums;
using FocusFlow.Infrastructure.Persistence;
using FocusFlow.Infrastructure.Services.TokenRefresh;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusFlow.Infrastructure.Services.Outlook
{
    public sealed class OutlookSyncService : IOutlookSyncService
    {
        private readonly FocusFlowDbContext _db;
        private readonly OutlookApiClient _apiClient;
        private readonly ILogger<OutlookSyncService> _logger;
        private readonly OutlookMessageParser _parser;
        private readonly OutlookTokenRefreshService _tokenRefreshService;

        public OutlookSyncService(
             FocusFlowDbContext db,
             IHttpClientFactory httpClientFactory,
             ILogger<OutlookSyncService> logger,
             OutlookMessageParser parser,
             OutlookTokenRefreshService tokenRefreshService)
        {
            _db = db;
            var httpClient = httpClientFactory.CreateClient("OutlookApi");
            _apiClient = new OutlookApiClient(httpClient);
            _logger = logger;
            _parser = parser;
            _tokenRefreshService = tokenRefreshService;
        }

        public async Task<EmailSyncResultDto> SyncLatestAsync(
            Guid emailAccountId,
            int maxCount = OutlookApiConstants.DefaultMaxCount,
            CancellationToken ct = default)
        {
            maxCount = Math.Clamp(
                maxCount,
                OutlookApiConstants.MinMaxCount,
                OutlookApiConstants.MaxMaxCount);

            var result = new SyncResult();

            try
            {
                // 1) Account + token check
                var account = await ValidateAccountAsync(emailAccountId, ct);
                if (account == null)
                {
                    result.Errors.Add($"EmailAccount {emailAccountId} not found or invalid.");
                    return result.ToDto();
                }

                // 2) Just-in-time refresh (threshold/expired)
                var tokenValid = await TokenRefreshHelper.EnsureValidTokenAsync(
                    emailAccountId,
                    EmailProvider.Outlook,
                    _db,
                    gmailRefreshService: null,
                    outlookRefreshService: _tokenRefreshService,
                    _logger,
                    ct);

                if (!tokenValid)
                {
                    result.Errors.Add("Failed to refresh access token. Please reconnect your account.");
                    return result.ToDto();
                }

                account = await ValidateAccountAsync(emailAccountId, ct);
                if (account == null)
                {
                    result.Errors.Add($"EmailAccount {emailAccountId} not found after token refresh.");
                    return result.ToDto();
                }

                // 3) Fetch messages + 401 retry once
                var messages = await ExecuteWith401RetryAsync(
                    emailAccountId,
                    ct,
                    async () =>
                    {
                        var a = await ValidateAccountAsync(emailAccountId, ct);
                        if (a == null) return new List<JsonElement>();
                        return await _apiClient.FetchMessagesAsync(a, maxCount, ct);
                    });

                if (messages == null)
                {
                    result.Errors.Add("Unauthorized while fetching Outlook messages. Please reconnect your account.");
                    return result.ToDto();
                }

                if (messages.Count == 0)
                {
                    _logger.LogInformation("No Outlook messages found for account {AccountId}", emailAccountId);
                    return result.ToDto();
                }

                var existingIds = await _db.Emails
                    .Where(e => e.EmailAccountId == emailAccountId && e.Provider == EmailProvider.Outlook)
                    .Where(e => e.ExternalMessageId != null)
                    .Select(e => e.ExternalMessageId!)
                    .ToListAsync(ct);

                var existingSet = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);

                foreach (var rawMessage in messages)
                {
                    var externalMessageId = ExtractMessageId(rawMessage);
                    if (externalMessageId == null)
                    {
                        result.Failed++;
                        result.Errors.Add("Encountered Outlook message without id.");
                        continue;
                    }

                    if (existingSet.Contains(externalMessageId))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var parsedEmail = _parser.ParseMessage(rawMessage, externalMessageId, account.Id);
                    if (parsedEmail == null)
                    {
                        result.Failed++;
                        result.Errors.Add($"Failed to parse Outlook message {externalMessageId}");
                        continue;
                    }

                    _db.Emails.Add(parsedEmail);
                    result.Added++;
                }

                if (result.Added > 0)
                    await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync failed for Outlook account {AccountId}", emailAccountId);
                result.Errors.Add($"Sync failed: {ex.Message}");
            }

            return result.ToDto();
        }

        private async Task<EmailAccount?> ValidateAccountAsync(Guid emailAccountId, CancellationToken ct)
        {
            var account = await _db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == emailAccountId, ct);

            if (account == null || account.Provider != EmailProvider.Outlook)
                return null;

            if (string.IsNullOrWhiteSpace(account.AccessToken))
                return null;

            return account;
        }

        private static string? ExtractMessageId(JsonElement messageElement)
        {
            if (!messageElement.TryGetProperty("id", out var idProperty))
                return null;

            var messageId = idProperty.GetString();
            return string.IsNullOrWhiteSpace(messageId) ? null : messageId;
        }

        private async Task<List<JsonElement>?> ExecuteWith401RetryAsync(
            Guid accountId,
            CancellationToken ct,
            Func<Task<List<JsonElement>>> action)
        {
            try
            {
                return await action();
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("401 Unauthorized. Force refresh + retry once for {AccountId}", accountId);

                var refreshed = await TokenRefreshHelper.ForceRefreshAsync(
                    accountId,
                    EmailProvider.Outlook,
                    gmailRefreshService: null,
                    outlookRefreshService: _tokenRefreshService,
                    _logger,
                    ct);

                if (!refreshed)
                    return null;

                return await action();
            }
        }

        private sealed class SyncResult
        {
            public int Added { get; set; }
            public int Skipped { get; set; }
            public int Failed { get; set; }
            public List<string> Errors { get; } = new();

            public EmailSyncResultDto ToDto() => new(Added, Skipped, Failed, Errors);
        }
    }
}
