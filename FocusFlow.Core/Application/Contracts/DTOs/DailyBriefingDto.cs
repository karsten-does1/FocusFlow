using System;
using System.Collections.Generic;

namespace FocusFlow.Core.Application.Contracts.DTOs
{
    public sealed record DailyBriefingDto(
        DateTime GeneratedAtUtc,
        IReadOnlyList<EmailDto> ImportantEmails,
        IReadOnlyList<FocusTaskDto> DueTasks,
        IReadOnlyList<ReminderDto> UpcomingReminders);
}
