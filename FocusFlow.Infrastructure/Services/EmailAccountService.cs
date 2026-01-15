using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Mappers;

namespace FocusFlow.Infrastructure.Services
{
    public sealed class EmailAccountService : IEmailAccountService
    {
        private readonly IEmailAccountRepository _repository;

        public EmailAccountService(IEmailAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<EmailAccountDto>> GetAllAsync(CancellationToken ct = default)
        {
            var entities = await _repository.GetAllAsync(ct);
            return entities.Select(e => MapToDto(e)).ToList();
        }

        public async Task<EmailAccountDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.GetAsync(id, ct);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<EmailAccountDto?> GetByEmailAddressAsync(string emailAddress, CancellationToken ct = default)
        {
            var entity = await _repository.GetByEmailAddressAsync(emailAddress, ct);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<Guid> AddAsync(EmailAccountDto dto, CancellationToken ct = default)
        {
            var entity = new EmailAccount(
                dto.Provider,
                dto.EmailAddress,
                dto.DisplayName,
                "", 
                "", 
                dto.AccessTokenExpiresUtc);

            return await _repository.AddAsync(entity, ct);
        }

        public async Task UpdateAsync(EmailAccountDto dto, CancellationToken ct = default)
        {
            var existingEntity = await _repository.GetForUpdateAsync(dto.Id, ct);
            if (existingEntity is null)
            {
                throw new InvalidOperationException($"EmailAccount with id {dto.Id} not found.");
            }

            existingEntity.UpdateDisplayName(dto.DisplayName);

            await _repository.UpdateAsync(existingEntity, ct);
        }

        public async Task UpdateTokensAsync(Guid id, string accessToken, DateTime expiresAtUtc, string? refreshToken = null, CancellationToken ct = default)
        {
            var existingEntity = await _repository.GetForUpdateAsync(id, ct);
            if (existingEntity is null)
            {
                throw new InvalidOperationException($"EmailAccount with id {id} not found.");
            }

            existingEntity.UpdateTokens(accessToken, expiresAtUtc, refreshToken);
            await _repository.UpdateAsync(existingEntity, ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            await _repository.DeleteAsync(id, ct);
        }

        private EmailAccountDto MapToDto(EmailAccount entity)
        {
            return EmailAccountMapper.ToDto(entity);
        }
    }
}

