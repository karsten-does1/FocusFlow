using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services
{
    public sealed class BriefingApi : IBriefingService
    {
        private readonly HttpClient _http;

        public BriefingApi(HttpClient http) => _http = http;

        public async Task<DailyBriefingDto> GetTodayAsync(CancellationToken ct = default)
        {
            var dto = await _http.GetFromJsonAsync<DailyBriefingDto>("/api/briefing", ct);

            return dto ?? new DailyBriefingDto(
                GeneratedAtUtc: DateTime.UtcNow,
                ImportantEmails: Array.Empty<EmailDto>(),
                DueTasks: Array.Empty<FocusTaskDto>(),
                UpcomingReminders: Array.Empty<ReminderDto>());
        }
    }
}
