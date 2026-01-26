using System;

namespace FocusFlow.Core.Domain.Entities
{
    public sealed class AppSettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int BriefingTasksHours { get; set; } = 48;
        public int BriefingRemindersHours { get; set; } = 24;
        public int BriefingEmailsDays { get; set; } = 2;

        public bool NotificationsEnabled { get; set; } = true;
        public int NotificationTickSeconds { get; set; } = 60;
        public int ReminderUpcomingWindowMinutes { get; set; } = 5;

        public bool BriefingNotificationsEnabled { get; set; } = true;
        public string BriefingTimeLocal { get; set; } = "09:00";
        public string BriefingSpeechMode { get; set; } = "Expanded";
    }
}
