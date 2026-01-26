namespace FocusFlow.Core.Application.Contracts.DTOs.Settings
{
    public sealed record BriefingSettingsDto(
        int TasksHours,
        int RemindersHours,
        int EmailsDays,
        string SpeechMode);
}
