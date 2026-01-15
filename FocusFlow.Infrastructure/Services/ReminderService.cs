using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Mappers;

namespace FocusFlow.Infrastructure.Services
{
    public sealed class ReminderService : IReminderService
    {
        private readonly IReminderRepository _repository;

        public ReminderService(IReminderRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> AddAsync(ReminderDto dto, CancellationToken ct = default)
        {
            var entity = new Reminder(dto.Title, dto.FireAtUtc, dto.RelatedTaskId, dto.RelatedEmailId);
            if (dto.Fired)
            {
                entity.MarkFired();
            }

            return await _repository.AddAsync(entity, ct);
        }

        public async Task<ReminderDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.GetAsync(id, ct);
            if (entity is null)
            {
                return null;
            }
            return ReminderMapper.ToDto(entity);
        }

        public async Task<IReadOnlyList<ReminderDto>> GetAllAsync(CancellationToken ct = default)
        {
            var entities = await _repository.GetAllAsync(ct);
            return entities.Select(ReminderMapper.ToDto).ToList();
        }

        public async Task<IReadOnlyList<ReminderDto>> UpcomingAsync(DateTime untilUtc, CancellationToken ct = default)
        {
            var entities = await _repository.UpcomingAsync(untilUtc, ct);
            return entities.Select(ReminderMapper.ToDto).ToList();
        }

        public async Task MarkFiredAsync(Guid id, CancellationToken ct = default)
        {
            await _repository.MarkFiredAsync(id, ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            await _repository.DeleteAsync(id, ct);
        }
    }
}

