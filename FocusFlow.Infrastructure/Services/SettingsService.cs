using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs.Settings;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusFlow.Infrastructure.Services
{
    public sealed class SettingsService : ISettingsService
    {
        private readonly FocusFlowDbContext _db;

        public SettingsService(FocusFlowDbContext db) => _db = db;

        public async Task<AppSettingsDto> GetAsync(CancellationToken ct = default)
        {
            var entity = await _db.AppSettings.FirstOrDefaultAsync(ct);

            if (entity is null)
            {
                entity = new AppSettings();
                _db.AppSettings.Add(entity);
                await _db.SaveChangesAsync(ct);
            }

            return Map(entity);
        }

        public async Task<AppSettingsDto> UpdateAsync(AppSettingsDto dto, CancellationToken ct = default)
        {
            var entity = await _db.AppSettings.FirstOrDefaultAsync(ct);

            if (entity is null)
            {
                entity = new AppSettings();
                _db.AppSettings.Add(entity);
            }

            entity.BriefingTasksHours = Clamp(dto.Briefing.TasksHours, 1, 24 * 30);
            entity.BriefingRemindersHours = Clamp(dto.Briefing.RemindersHours, 1, 24 * 30);
            entity.BriefingEmailsDays = Clamp(dto.Briefing.EmailsDays, 1, 30);


            entity.NotificationsEnabled = dto.Notifications.Enabled;
            entity.NotificationTickSeconds = Clamp(dto.Notifications.TickSeconds, 10, 300);
            entity.ReminderUpcomingWindowMinutes = Clamp(dto.Notifications.ReminderUpcomingWindowMinutes, 1, 60);

            entity.BriefingNotificationsEnabled = dto.Notifications.BriefingEnabled;
            entity.BriefingTimeLocal = NormalizeTime(dto.Notifications.BriefingTimeLocal);

            await _db.SaveChangesAsync(ct);
            return Map(entity);
        }

        private static AppSettingsDto Map(AppSettings e)
            => new(
                new BriefingSettingsDto(e.BriefingTasksHours, e.BriefingRemindersHours, e.BriefingEmailsDays),
                new NotificationSettingsDto(
                    Enabled: e.NotificationsEnabled,
                    TickSeconds: e.NotificationTickSeconds,
                    ReminderUpcomingWindowMinutes: e.ReminderUpcomingWindowMinutes,
                    BriefingEnabled: e.BriefingNotificationsEnabled,
                    BriefingTimeLocal: e.BriefingTimeLocal
                )
            );

        private static int Clamp(int v, int min, int max)
            => v < min ? min : (v > max ? max : v);

        private static string NormalizeTime(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "09:00";
            // Expect HH:mm; if parsing fails, fallback.
            return System.TimeSpan.TryParse(v.Trim(), out var t)
                ? t.ToString(@"hh\:mm")
                : "09:00";
        }
    }
}
