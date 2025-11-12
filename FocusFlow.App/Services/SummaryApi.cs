using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services
{
    public sealed class SummaryApi : ISummaryService
    {
        private readonly HttpClient _http;
        public SummaryApi(HttpClient http) => _http = http;

        public async Task UpsertAsync(Guid emailId, string text, CancellationToken ct = default)
        {
            var payload = new { emailId, text };
            var resp = await _http.PostAsJsonAsync("/api/summaries", payload, ct);
            resp.EnsureSuccessStatusCode();
        }

        public Task<string?> GetTextAsync(Guid emailId, CancellationToken ct = default)
            => _http.GetFromJsonAsync<string>($"/api/summaries/{emailId}", ct);
    }
}

