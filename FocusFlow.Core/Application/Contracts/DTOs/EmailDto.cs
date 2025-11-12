using System;

namespace FocusFlow.Core.Application.Contracts.DTOs
{
    public sealed record EmailDto(Guid Id, string From, string Subject, string BodyText, DateTime ReceivedUtc, int PriorityScore);
}

