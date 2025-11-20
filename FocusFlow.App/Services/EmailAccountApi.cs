using System;
using System.Net.Http;
using System.Net.Http.Json;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services
{
    public sealed class EmailAccountApi : IEmailAccountService
    {
        private readonly HttpClient _http;
        public EmailAccountApi(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<EmailAccountDto>> GetAllAsync(CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<IReadOnlyList<EmailAccountDto>>("/api/emailaccounts", ct) ?? Array.Empty<EmailAccountDto>();
        }

        public async Task<EmailAccountDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<EmailAccountDto>($"/api/emailaccounts/{id}", ct);
        }

        public async Task<EmailAccountDto?> GetByEmailAddressAsync(string emailAddress, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<EmailAccountDto>($"/api/emailaccounts/by-email/{Uri.EscapeDataString(emailAddress)}", ct);
        }

        public async Task<Guid> AddAsync(EmailAccountDto dto, CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync("/api/emailaccounts", dto, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        }

        public async Task UpdateAsync(EmailAccountDto dto, CancellationToken ct = default)
        {
            var resp = await _http.PutAsJsonAsync($"/api/emailaccounts/{dto.Id}", dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task UpdateTokensAsync(Guid id, string accessToken, DateTime expiresAtUtc, string? refreshToken = null, CancellationToken ct = default)
        {
            var request = new { AccessToken = accessToken, ExpiresAtUtc = expiresAtUtc, RefreshToken = refreshToken };
            var resp = await _http.PutAsJsonAsync($"/api/emailaccounts/{id}/tokens", request, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var resp = await _http.DeleteAsync($"/api/emailaccounts/{id}", ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}

