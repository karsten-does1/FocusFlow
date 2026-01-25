using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.DTOs.Ai;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Core.Domain.Enums;
using FocusFlow.Infrastructure.Persistence;
using FocusFlow.Infrastructure.Services.TokenRefresh;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusFlow.Infrastructure.Services.Gmail
{
    public sealed class GmailSyncService : IGmailSyncService
    {
        private readonly FocusFlowDbContext _db;
        private readonly GmailApiClient _apiClient;
        private readonly ILogger<GmailSyncService> _logger;
        private readonly IEmailMessageParser<JsonElement> _messageParser;
        private readonly IAiService _aiService;
        private readonly GmailTokenRefreshService _tokenRefreshService;

        public GmailSyncService(
            FocusFlowDbContext db,
            IHttpClientFactory httpClientFactory,
            ILogger<GmailSyncService> logger,
            IEmailMessageParser<JsonElement> messageParser,
            IAiService aiService,
            GmailTokenRefreshService tokenRefreshService)
        {
            _db = db;

            var httpClient = httpClientFactory.CreateClient("GmailApi");
            _apiClient = new GmailApiClient(httpClient);

            _logger = logger;
            _messageParser = messageParser;
            _aiService = aiService;
            _tokenRefreshService = tokenRefreshService;
        }

        public async Task<EmailSyncResultDto> SyncLatestAsync(
            Guid emailAccountId,
            int maxCount = GmailApiConstants.DefaultMaxCount,
            CancellationToken ct = default)
        {
            maxCount = Math.Clamp(
                maxCount,
                GmailApiConstants.MinMaxCount,
                GmailApiConstants.MaxMaxCount
            );

            var result = new SyncResult();

            try
            {
                var account = await EnsureAccountAndTokenValidAsync(emailAccountId, result, ct);
                if (account == null)
                    return result.ToDto();

                var messageIds = await FetchMessageIdsWithRetryAsync(emailAccountId, maxCount, result, ct);
                if (messageIds == null || messageIds.Count == 0)
                    return result.ToDto();

                var existingIds = await GetExistingMessageIdsAsync(emailAccountId, ct);

                foreach (var messageId in messageIds)
                {
                    if (existingIds.Contains(messageId))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var email = await FetchAndProcessMessageAsync(messageId, account, emailAccountId, result, ct);
                    if (email != null)
                    {
                        await AnalyzeAndSaveEmailAsync(email, messageId, result, ct);
                    }
                }

                if (result.Added > 0)
                    await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync failed for account {AccountId}", emailAccountId);
                result.Errors.Add($"Sync failed: {ex.Message}");
            }

            return result.ToDto();
        }

        private async Task<EmailAccount?> EnsureAccountAndTokenValidAsync(
            Guid emailAccountId,
            SyncResult result,
            CancellationToken ct)
        {
            var account = await ValidateAccountAsync(emailAccountId, ct);
            if (account == null)
            {
                result.Errors.Add($"EmailAccount {emailAccountId} not found or invalid.");
                return null;
            }

            var tokenValid = await TokenRefreshHelper.EnsureValidTokenAsync(
                emailAccountId,
                EmailProvider.Gmail,
                _db,
                gmailRefreshService: _tokenRefreshService,
                outlookRefreshService: null,
                _logger,
                ct);

            if (!tokenValid)
            {
                result.Errors.Add("Failed to refresh access token. Please reconnect your account.");
                return null;
            }

            account = await ValidateAccountAsync(emailAccountId, ct);
            if (account == null)
            {
                result.Errors.Add($"EmailAccount {emailAccountId} not found after token refresh.");
                return null;
            }

            return account;
        }

        private async Task<List<string>?> FetchMessageIdsWithRetryAsync(
            Guid emailAccountId,
            int maxCount,
            SyncResult result,
            CancellationToken ct)
        {
            var messageIds = await ExecuteWith401RetryAsync(
                emailAccountId,
                ct,
                async () =>
                {
                    var a = await ValidateAccountAsync(emailAccountId, ct);
                    if (a == null) return new List<string>();

                    return await _apiClient.FetchMessageIdsAsync(
                        a,
                        GmailApiConstants.DefaultSearchQuery,
                        maxCount,
                        ct);
                });

            if (messageIds == null)
            {
                result.Errors.Add("Unauthorized while fetching Gmail message ids. Please reconnect your account.");
                return null;
            }

            if (messageIds.Count == 0)
            {
                _logger.LogInformation("No new messages found for account {AccountId}", emailAccountId);
            }

            return messageIds;
        }

        private async Task<HashSet<string>> GetExistingMessageIdsAsync(
            Guid emailAccountId,
            CancellationToken ct)
        {
            var existingIds = await _db.Emails
                .Where(e => e.EmailAccountId == emailAccountId && e.Provider == EmailProvider.Gmail)
                .Where(e => e.ExternalMessageId != null)
                .Select(e => e.ExternalMessageId!)
                .ToListAsync(ct);

            return new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<Email?> FetchAndProcessMessageAsync(
            string messageId,
            EmailAccount account,
            Guid emailAccountId,
            SyncResult result,
            CancellationToken ct)
        {
            var fetched = await ExecuteTupleWith401RetryAsync(
                emailAccountId,
                ct,
                async () =>
                {
                    var a = await ValidateAccountAsync(emailAccountId, ct);
                    if (a == null) return (Email: (Email?)null, Skipped: false);

                    return await FetchAndParseMessageAsync(a, messageId, ct);
                });

            if (fetched == null)
            {
                result.Failed++;
                result.Errors.Add($"Unauthorized while fetching Gmail message {messageId}. Please reconnect.");
                return null;
            }

            var (email, skippedByLabel) = fetched.Value;

            if (skippedByLabel)
            {
                result.Skipped++;
                return null;
            }

            if (email == null)
            {
                result.Failed++;
                result.Errors.Add($"Failed to fetch or parse message {messageId}");
                return null;
            }

            return email;
        }

        private async Task AnalyzeAndSaveEmailAsync(
            Email email,
            string messageId,
            SyncResult result,
            CancellationToken ct)
        {
            try
            {
                var request = new AnalyzeRequestDto(
                    Subject: email.Subject ?? "",
                    Body: email.BodyText ?? ""
                );

                var analysis = await _aiService.AnalyzeEmailAsync(request, ct);

                email.SetAiAnalysis(
                    analysis.Priority,
                    analysis.Category,
                    analysis.Action);

                if (!string.IsNullOrWhiteSpace(analysis.Summary))
                {
                    var summaryEntity = new Summary(email.Id, analysis.Summary);
                    _db.Summaries.Add(summaryEntity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "AI analyse mislukt voor bericht {MessageId}: {Message}",
                    messageId,
                    ex.Message);
            }

            _db.Emails.Add(email);
            result.Added++;
        }

        private async Task<EmailAccount?> ValidateAccountAsync(Guid emailAccountId, CancellationToken ct)
        {
            var account = await _db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == emailAccountId, ct);

            if (account == null || account.Provider != EmailProvider.Gmail) return null;
            if (string.IsNullOrWhiteSpace(account.AccessToken)) return null;
            return account;
        }

        private async Task<(Email? Email, bool Skipped)> FetchAndParseMessageAsync(
            EmailAccount account,
            string messageId,
            CancellationToken ct)
        {
            var messageRoot = await _apiClient.FetchMessageAsync(account, messageId, ct);
            if (!messageRoot.HasValue) return (null, false);

            if (GmailLabelFilter.ShouldSkip(messageRoot.Value))
            {
                _logger.LogInformation("Skipped Gmail message {MessageId} due to labels", messageId);
                return (null, true);
            }

            var email = _messageParser.ParseMessage(messageRoot.Value, messageId, account.Id);
            return (email, false);
        }

        private async Task<T?> ExecuteWith401RetryAsync<T>(
            Guid accountId,
            CancellationToken ct,
            Func<Task<T>> action)
            where T : class
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
                    EmailProvider.Gmail,
                    gmailRefreshService: _tokenRefreshService,
                    outlookRefreshService: null,
                    _logger,
                    ct);

                if (!refreshed)
                    return null;

                return await action();
            }
        }

        private async Task<(Email? Email, bool Skipped)?> ExecuteTupleWith401RetryAsync(
            Guid accountId,
            CancellationToken ct,
            Func<Task<(Email? Email, bool Skipped)>> action)
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
                    EmailProvider.Gmail,
                    gmailRefreshService: _tokenRefreshService,
                    outlookRefreshService: null,
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

            public EmailSyncResultDto ToDto()
            {
                return new EmailSyncResultDto(
                    Added,
                    Skipped,
                    Failed,
                    Errors);
            }
        }
    }
}
