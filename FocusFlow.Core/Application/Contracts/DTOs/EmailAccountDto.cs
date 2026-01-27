using System;
using FocusFlow.Core.Domain.Enums;

namespace FocusFlow.Core.Application.Contracts.DTOs
{
    public sealed record class EmailAccountDto(
        Guid Id,
        string DisplayName,
        string EmailAddress,
        EmailProvider Provider,
        DateTime AccessTokenExpiresUtc,
        DateTime ConnectedAtUtc)
    {
        public DateTime AccessTokenExpiresLocal => AccessTokenExpiresUtc.ToLocalTime();
        public DateTime ConnectedAtLocal => ConnectedAtUtc.ToLocalTime();
    }
}
