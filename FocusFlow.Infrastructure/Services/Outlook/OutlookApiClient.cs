using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Infrastructure.Services.Outlook
{
    public sealed class OutlookApiClient
    {
        private readonly HttpClient _http;

        public OutlookApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<JsonElement>> FetchMessagesAsync(
            EmailAccount account,
            int maxCount,
            CancellationToken ct)
        {
            var url = string.Format(OutlookApiConstants.MessagesEndpoint, maxCount);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);

            var response = await _http.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Outlook API returned 401 Unauthorized");

            response.EnsureSuccessStatusCode();

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct),
                cancellationToken: ct);

            if (!json.RootElement.TryGetProperty("value", out var valueArray) ||
                valueArray.ValueKind != JsonValueKind.Array)
            {
                return new List<JsonElement>();
            }

            var result = new List<JsonElement>();
            foreach (var item in valueArray.EnumerateArray())
            {
                result.Add(item.Clone());
            }

            return result;
        }
    }
}
