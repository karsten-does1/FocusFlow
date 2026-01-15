using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Infrastructure.Mappers
{
    public static class EmailAccountMapper
    {
        public static EmailAccountDto ToDto(EmailAccount entity) =>
            new EmailAccountDto(
                entity.Id,
                entity.DisplayName,
                entity.EmailAddress,
                entity.Provider,
                entity.AccessTokenExpiresUtc,
                entity.ConnectedAtUtc);
    }
}

