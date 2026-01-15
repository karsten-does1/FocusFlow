using System;

namespace FocusFlow.Core.Application.Contracts.DTOs
{
    public sealed record SummaryDto(
        Guid Id, 
        Guid EmailId, 
        string Text, 
        DateTime CreatedUtc);
}

