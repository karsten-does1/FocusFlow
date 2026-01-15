using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.App.Services
{
    public sealed class EmailSyncApi
    {
        private readonly HttpClient _http;
        public EmailSyncApi(HttpClient http) => _http = http;

        public async Task<EmailSyncResultDto> SyncGmailAsync(Guid accountId, int maxCount, CancellationToken ct = default)
        {
            var response = await _http.PostAsync($"/api/email-sync/gmail/{accountId}?maxCount={maxCount}", null, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EmailSyncResultDto>(cancellationToken: ct) 
                ?? throw new InvalidOperationException("Failed to deserialize sync result");
        }

        public async Task<EmailSyncResultDto> SyncOutlookAsync(Guid accountId, int maxCount, CancellationToken ct = default)
        {
            var response = await _http.PostAsync($"/api/email-sync/microsoft/{accountId}?maxCount={maxCount}", null, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EmailSyncResultDto>(cancellationToken: ct) 
                ?? throw new InvalidOperationException("Failed to deserialize sync result");
        }
    }
}

