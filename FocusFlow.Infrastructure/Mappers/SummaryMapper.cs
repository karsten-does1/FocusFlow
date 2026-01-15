using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Infrastructure.Mappers
{
    public static class SummaryMapper
    {
        public static SummaryDto ToDto(Summary entity)
        {
            return new SummaryDto(
                entity.Id,
                entity.EmailId,
                entity.Text,
                entity.CreatedUtc
            );
        }
    }
}

