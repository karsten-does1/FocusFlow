using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services
{
    public sealed class ReminderApi : IReminderService
    {
        private readonly HttpClient _http;
        public ReminderApi(HttpClient http) => _http = http;

        public async Task<Guid> AddAsync(ReminderDto dto, CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync("/api/reminders", dto, ct);
            resp.EnsureSuccessStatusCode();
            var id = await resp.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
            return id;
        }

        public Task<ReminderDto?> GetAsync(Guid id, CancellationToken ct = default)
            => _http.GetFromJsonAsync<ReminderDto>($"/api/reminders/{id}", ct);

        public async Task<IReadOnlyList<ReminderDto>> GetAllAsync(CancellationToken ct = default)
            => await _http.GetFromJsonAsync<IReadOnlyList<ReminderDto>>("/api/reminders", ct) ?? Array.Empty<ReminderDto>();

        public Task<IReadOnlyList<ReminderDto>?> RawUpcomingAsync(DateTime untilUtc, CancellationToken ct = default)
            => _http.GetFromJsonAsync<IReadOnlyList<ReminderDto>>($"/api/reminders/upcoming?untilUtc={untilUtc:o}", ct);

        public async Task<IReadOnlyList<ReminderDto>> UpcomingAsync(DateTime untilUtc, CancellationToken ct = default)
            => await RawUpcomingAsync(untilUtc, ct) ?? Array.Empty<ReminderDto>();

        public async Task MarkFiredAsync(Guid id, CancellationToken ct = default)
        {
            var resp = await _http.PostAsync($"/api/reminders/{id}/fired", null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var resp = await _http.DeleteAsync($"/api/reminders/{id}", ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}

