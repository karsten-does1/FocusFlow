using System;
using System.Net.Http;
using System.Net.Http.Json;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services
{
    public sealed class EmailApi : IEmailService
    {
        private readonly HttpClient _http;
        public EmailApi(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<EmailDto>> GetLatestAsync(string? q, CancellationToken ct = default)
        {
            var url = "/api/emails";
            if (!string.IsNullOrWhiteSpace(q))
                url += $"?q={Uri.EscapeDataString(q)}";

            return await _http.GetFromJsonAsync<IReadOnlyList<EmailDto>>(url, ct) ?? Array.Empty<EmailDto>();
        }

        public Task<EmailDto?> GetAsync(Guid id, CancellationToken ct = default)
            => _http.GetFromJsonAsync<EmailDto>($"/api/emails/{id}", ct);

        public async Task<Guid> AddAsync(EmailDto dto, CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync("/api/emails", dto, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        }
    }
}
