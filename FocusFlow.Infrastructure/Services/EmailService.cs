using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Mappers;

namespace FocusFlow.Infrastructure.Services
{
    public sealed class EmailService : IEmailService
    {
        private readonly IEmailRepository _repository;

        public EmailService(IEmailRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<EmailDto>> GetLatestAsync(string? query, CancellationToken ct = default)
        {
            var entities = await _repository.GetLatestAsync(query, ct);
            return entities.Select(EmailMapper.ToDto).ToList();
        }

        public async Task<EmailDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.GetAsync(id, ct);
            if (entity is null)
            {
                return null;
            }
            return EmailMapper.ToDto(entity);
        }

        public async Task<Guid> AddAsync(EmailDto dto, CancellationToken ct = default)
        {
            var entity = new Email(dto.From, dto.Subject, dto.BodyText, dto.ReceivedUtc);
            entity.SetPriority(dto.PriorityScore);

            return await _repository.AddAsync(entity, ct);
        }

        public async Task UpdateAsync(EmailDto dto, CancellationToken ct = default)
        {
            var existingEntity = await _repository.GetForUpdateAsync(dto.Id, ct);
            if (existingEntity is null)
            {
                throw new InvalidOperationException($"Email with id {dto.Id} not found.");
            }

            existingEntity.Update(dto.From, dto.Subject, dto.BodyText, dto.ReceivedUtc, dto.PriorityScore);
            await _repository.UpdateAsync(existingEntity, ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            await _repository.DeleteAsync(id, ct);
        }
    }
}

