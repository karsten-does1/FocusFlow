using System;

namespace FocusFlow.Core.Application.Contracts.DTOs
{
    public sealed record ReminderDto(Guid Id, string Title, DateTime FireAtUtc, bool Fired, Guid? RelatedTaskId, Guid? RelatedEmailId);
}

