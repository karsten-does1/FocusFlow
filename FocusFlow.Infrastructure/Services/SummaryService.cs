using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Mappers;

namespace FocusFlow.Infrastructure.Services
{
    public sealed class SummaryService : ISummaryService
    {
        private readonly ISummaryRepository _repository;

        public SummaryService(ISummaryRepository repository)
        {
            _repository = repository;
        }

        public async Task<SummaryDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.GetAsync(id, ct);
            if (entity is null)
            {
                return null;
            }
            return SummaryMapper.ToDto(entity);
        }

        public async Task<SummaryDto?> GetByEmailIdAsync(Guid emailId, CancellationToken ct = default)
        {
            var entity = await _repository.GetByEmailIdAsync(emailId, ct);
            if (entity is null)
            {
                return null;
            }
            return SummaryMapper.ToDto(entity);
        }

        public async Task<Guid> AddAsync(SummaryDto dto, CancellationToken ct = default)
        {
            var entity = new Summary(dto.EmailId, dto.Text);
            return await _repository.AddAsync(entity, ct);
        }

        public async Task UpdateAsync(SummaryDto dto, CancellationToken ct = default)
        {
            var existingEntity = await _repository.GetForUpdateAsync(dto.Id, ct);
            if (existingEntity is null)
            {
                throw new InvalidOperationException($"Summary with id {dto.Id} not found.");
            }
            existingEntity.UpdateText(dto.Text);
            await _repository.UpdateAsync(existingEntity, ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            await _repository.DeleteAsync(id, ct);
        }
    }
}

