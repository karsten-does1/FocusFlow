using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface IEmailAccountService
    {
        Task<IReadOnlyList<EmailAccountDto>> GetAllAsync(CancellationToken ct = default);
        Task<EmailAccountDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<EmailAccountDto?> GetByEmailAddressAsync(string emailAddress, CancellationToken ct = default);
        Task<Guid> AddAsync(EmailAccountDto dto, CancellationToken ct = default);
        Task UpdateAsync(EmailAccountDto dto, CancellationToken ct = default);
        Task UpdateTokensAsync(Guid id, string accessToken, DateTime expiresAtUtc, string? refreshToken = null, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

