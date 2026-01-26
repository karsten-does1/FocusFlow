using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs.Settings;

namespace FocusFlow.App.Services
{
    public sealed class SettingsApi
    {
        private readonly HttpClient _http;

        public SettingsApi(HttpClient http) => _http = http;

        public async Task<AppSettingsDto> GetAsync(CancellationToken ct = default)
        {
            var dto = await _http.GetFromJsonAsync<AppSettingsDto>("/api/settings", ct);
            return dto ?? Default();
        }

        public async Task<AppSettingsDto> UpdateAsync(AppSettingsDto dto, CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync("/api/settings", dto, ct);
            resp.EnsureSuccessStatusCode();

            return (await resp.Content.ReadFromJsonAsync<AppSettingsDto>(cancellationToken: ct))
                   ?? dto;
        }

        public static AppSettingsDto Default()
            => new(
                new BriefingSettingsDto(48, 24, 2, "Expanded"),
                new NotificationSettingsDto(
                    Enabled: true,
                    TickSeconds: 60,
                    ReminderUpcomingWindowMinutes: 5,
                    BriefingEnabled: true,
                    BriefingTimeLocal: "09:00"));
    }
}
