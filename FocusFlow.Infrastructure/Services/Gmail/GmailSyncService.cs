using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Core.Domain.Enums;
using FocusFlow.Infrastructure.Persistence;
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

        public GmailSyncService(
            FocusFlowDbContext db,
            IHttpClientFactory httpClientFactory,
            ILogger<GmailSyncService> logger,
            IEmailMessageParser<JsonElement> messageParser)
        {
            _db = db;
            var httpClient = httpClientFactory.CreateClient("GmailApi");
            _apiClient = new GmailApiClient(httpClient);
            _logger = logger;
            _messageParser = messageParser;
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
                var account = await ValidateAccountAsync(emailAccountId, ct);
                if (account == null)
                {
                    result.Errors.Add($"EmailAccount {emailAccountId} not found or invalid.");
                    return result.ToDto();
                }

                var messageIds = await _apiClient.FetchMessageIdsAsync(
                    account,
                    GmailApiConstants.DefaultSearchQuery,
                    maxCount,
                    ct);

                if (messageIds.Count == 0)
                {
                    _logger.LogInformation("No new messages found for account {AccountId}", emailAccountId);
                    return result.ToDto();
                }

                var existingIds = await _db.Emails
                    .Where(e => e.EmailAccountId == emailAccountId && e.Provider == EmailProvider.Gmail)
                    .Where(e => e.ExternalMessageId != null)
                    .Select(e => e.ExternalMessageId!)
                    .ToListAsync(ct);

                var existingSet = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);

                foreach (var messageId in messageIds)
                {
                    if (existingSet.Contains(messageId))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var (email, skippedByLabel) = await FetchAndParseMessageAsync(account, messageId, ct);

                    if (skippedByLabel)
                    {
                        
                        result.Skipped++;
                        continue;
                    }

                    if (email == null)
                    {
                        result.Failed++;
                        result.Errors.Add($"Failed to fetch or parse message {messageId}");
                        continue;
                    }

                    _db.Emails.Add(email);
                    result.Added++;
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

        private async Task<EmailAccount?> ValidateAccountAsync(Guid emailAccountId, CancellationToken ct)
        {
            var account = await _db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == emailAccountId, ct);

            if (account == null || account.Provider != EmailProvider.Gmail)
                return null;

            if (string.IsNullOrWhiteSpace(account.AccessToken))
                return null;

            return account;
        }

        private async Task<(Email? Email, bool Skipped)> FetchAndParseMessageAsync(
            EmailAccount account,
            string messageId,
            CancellationToken ct)
        {
            var messageRoot = await _apiClient.FetchMessageAsync(account, messageId, ct);
            if (!messageRoot.HasValue)
                return (null, false);

            if (GmailLabelFilter.ShouldSkip(messageRoot.Value))
            {
                _logger.LogInformation("Skipped Gmail message {MessageId} due to labels", messageId);
                return (null, true);
            }

            var email = _messageParser.ParseMessage(messageRoot.Value, messageId, account.Id);
            return (email, false);
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