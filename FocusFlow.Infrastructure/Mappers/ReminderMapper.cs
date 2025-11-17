using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Infrastructure.Mappers
{
    public static class ReminderMapper
    {
        public static ReminderDto ToDto(Reminder entity) =>
            new ReminderDto(
                entity.Id,
                entity.Title,
                entity.FireAtUtc,
                entity.Fired,
                entity.RelatedTaskId,
                entity.RelatedEmailId);
    }
}

