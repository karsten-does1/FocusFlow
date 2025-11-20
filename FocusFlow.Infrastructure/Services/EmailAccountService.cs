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
            return entities.Select(EmailAccountMapper.ToDto).ToList();
        }

        public async Task<EmailAccountDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _repository.GetAsync(id, ct);
            if (entity is null)
            {
                return null;
            }
            return EmailAccountMapper.ToDto(entity);
        }

        public async Task<EmailAccountDto?> GetByEmailAddressAsync(string emailAddress, CancellationToken ct = default)
        {
            var entity = await _repository.GetByEmailAddressAsync(emailAddress, ct);
            if (entity is null)
            {
                return null;
            }
            return EmailAccountMapper.ToDto(entity);
        }

        public async Task<Guid> AddAsync(EmailAccountDto dto, CancellationToken ct = default)
        {
            var entity = new EmailAccount(
                dto.Provider,
                dto.EmailAddress,
                dto.DisplayName,
                "", // AccessToken - wordt later gezet via UpdateTokens
                "", // RefreshToken
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

            // Note: DisplayName en EmailAddress kunnen niet via Update worden gewijzigd
            // omdat EmailAccount geen Update method heeft voor deze properties
            // Als je dit nodig hebt, moet je een Update method toevoegen aan EmailAccount entity
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
    }
}

