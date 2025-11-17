using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface IEmailService
    {
        Task<IReadOnlyList<EmailDto>> GetLatestAsync(string? q, CancellationToken ct = default);
        Task<EmailDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<Guid> AddAsync(EmailDto dto, CancellationToken ct = default);
        Task UpdateAsync(EmailDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

