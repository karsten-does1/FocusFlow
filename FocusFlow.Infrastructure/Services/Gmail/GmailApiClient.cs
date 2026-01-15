using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Infrastructure.Services.Gmail
{
    public sealed class GmailApiClient
    {
        private readonly HttpClient _http;

        public GmailApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<string>> FetchMessageIdsAsync(
            EmailAccount account,
            string searchQuery,
            int maxCount,
            CancellationToken ct)
        {
            var query = Uri.EscapeDataString(searchQuery);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GmailApiConstants.MessagesEndpoint}?maxResults={maxCount}&q={query}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);

            var response = await _http.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Gmail API returned 401 Unauthorized");

            response.EnsureSuccessStatusCode();

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            if (!json.RootElement.TryGetProperty("messages", out var messagesArray))
                return new List<string>();

            return messagesArray.EnumerateArray()
                .Select(messageElement => messageElement.GetProperty("id").GetString())
                .Where(messageId => !string.IsNullOrWhiteSpace(messageId))
                .Cast<string>()
                .ToList();
        }

        public async Task<JsonElement?> FetchMessageAsync(
            EmailAccount account,
            string messageId,
            CancellationToken ct)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GmailApiConstants.MessagesEndpoint}/{messageId}?format=full");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);

            var response = await _http.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Gmail API returned 401 Unauthorized");

            if (!response.IsSuccessStatusCode)
                return null;

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            return json.RootElement.Clone();
        }
    }
}
