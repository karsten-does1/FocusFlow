using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Infrastructure.Mappers
{
    public static class EmailMapper
    {
        public static EmailDto ToDto(Email entity) =>
            new EmailDto(
                entity.Id,
                entity.From,
                entity.Subject,
                entity.BodyText,
                entity.ReceivedUtc,
                entity.PriorityScore);
    }
}

