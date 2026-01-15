using System;
using FocusFlow.Core.Domain.Enums;

namespace FocusFlow.Core.Application.Contracts.DTOs
{
    public sealed record EmailDto(
        Guid Id,
        string From,
        string Subject,
        string BodyText,
        DateTime ReceivedUtc,
        int PriorityScore,
        string Category,           
        string SuggestedAction,    
        EmailProvider Provider = EmailProvider.Unknown,
        string? ExternalMessageId = null,
        Guid? EmailAccountId = null);
}

