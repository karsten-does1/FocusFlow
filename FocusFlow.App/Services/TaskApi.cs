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
    public sealed class TaskApi : ITaskService
    {
        private readonly HttpClient _http;
        public TaskApi(HttpClient http) => _http = http;

        public Task<IReadOnlyList<FocusTaskDto>?> RawListAsync(bool? done, CancellationToken ct = default)
        {
            var url = "/api/tasks";
            if (done is not null) url += $"?done={done}";
            return _http.GetFromJsonAsync<IReadOnlyList<FocusTaskDto>>(url, ct);
        }

        public async Task<IReadOnlyList<FocusTaskDto>> ListAsync(bool? done = null, CancellationToken ct = default)
            => await RawListAsync(done, ct) ?? Array.Empty<FocusTaskDto>();

        public Task<FocusTaskDto?> GetAsync(Guid id, CancellationToken ct = default)
            => _http.GetFromJsonAsync<FocusTaskDto>($"/api/tasks/{id}", ct);

        public async Task<Guid> AddAsync(FocusTaskDto dto, CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync("/api/tasks", dto, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        }

        public async Task UpdateAsync(FocusTaskDto dto, CancellationToken ct = default)
        {
            var resp = await _http.PutAsJsonAsync($"/api/tasks/{dto.Id}", dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var resp = await _http.DeleteAsync($"/api/tasks/{id}", ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}

