using System;

namespace FocusFlow.Core.Application.Contracts.DTOs
{
    public sealed record FocusTaskDto(
        Guid Id,
        string Title,
        string? Notes,
        DateTime? DueUtc,
        bool IsDone,
        Guid? RelatedEmailId)
    {
        public DateTime? DueLocal => DueUtc?.ToLocalTime();
    }
}

