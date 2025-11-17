using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.Core.Application.Contracts.Repositories
{
    public interface IEmailRepository
    {
        Task<IReadOnlyList<EmailDto>> GetLatestAsync(string? search, CancellationToken ct = default);
        Task<EmailDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<Guid> AddAsync(EmailDto dto, CancellationToken ct = default);
        Task UpdateAsync(EmailDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

