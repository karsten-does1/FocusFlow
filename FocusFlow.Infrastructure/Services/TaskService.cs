using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Mappers;

namespace FocusFlow.Infrastructure.Services
{
    public sealed class TaskService : ITaskService
    {
        private readonly ITaskRepository _repository;

        public TaskService(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> AddAsync(FocusTaskDto dto, CancellationToken ct = default)
        {
            var entity = new FocusTask(dto.Title, dto.Notes, dto.DueUtc, dto.RelatedEmailId);
            if (dto.IsDone)
            {
                entity.Complete();
            }

            return await _repository.AddAsync(entity, ct);
        }

        public async Task UpdateAsync(FocusTaskDto dto, CancellationToken ct = default)
        {
            var existingEntity = await _repository.GetForUpdateAsync(dto.Id, ct);
            if (existingEntity is null)
            {
                throw new InvalidOperationException($"Task with id {dto.Id} not found.");
            }

            existingEntity.Update(dto.Title, dto.Notes, dto.DueUtc, dto.IsDone, dto.RelatedEmailId);
            await _repository.UpdateAsync(existingEntity, ct);
        }

        public async Task<FocusTaskDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.GetAsync(id, ct);
            if (entity is null)
            {
                return null;
            }
            return TaskMapper.ToDto(entity);
        }

        public async Task<IReadOnlyList<FocusTaskDto>> ListAsync(bool? done = null, CancellationToken ct = default)
        {
            var entities = await _repository.ListAsync(done, ct);
            return entities.Select(TaskMapper.ToDto).ToList();
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            await _repository.DeleteAsync(id, ct);
        }
    }
}

