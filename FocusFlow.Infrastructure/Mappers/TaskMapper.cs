using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Infrastructure.Mappers
{
    public static class TaskMapper
    {
        public static FocusTaskDto ToDto(FocusTask entity) =>
            new FocusTaskDto(
                entity.Id,
                entity.Title,
                entity.Notes,
                entity.DueUtc,
                entity.IsDone,
                entity.RelatedEmailId);
    }
}

