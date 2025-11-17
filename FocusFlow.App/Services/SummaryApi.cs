using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services
{
    public sealed class SummaryApi : ISummaryService
    {
        private readonly HttpClient _http;
        public SummaryApi(HttpClient http) => _http = http;

        public Task<SummaryDto?> GetAsync(Guid id, CancellationToken ct = default)
            => _http.GetFromJsonAsync<SummaryDto>($"/api/summaries/{id}", ct);

        public Task<SummaryDto?> GetByEmailIdAsync(Guid emailId, CancellationToken ct = default)
            => _http.GetFromJsonAsync<SummaryDto>($"/api/summaries/email/{emailId}", ct);

        public async Task<Guid> AddAsync(SummaryDto dto, CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync("/api/summaries", dto, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        }

        public async Task UpdateAsync(SummaryDto dto, CancellationToken ct = default)
        {
            var resp = await _http.PutAsJsonAsync($"/api/summaries/{dto.Id}", dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var resp = await _http.DeleteAsync($"/api/summaries/{id}", ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}

