namespace FocusFlow.Core.Application.Contracts.DTOs.Settings
{
    public sealed record NotificationSettingsDto(
        bool Enabled,
        int TickSeconds,
        int ReminderUpcomingWindowMinutes,
        bool BriefingEnabled,
        string BriefingTimeLocal);
}
