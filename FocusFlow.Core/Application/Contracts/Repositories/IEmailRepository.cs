using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Core.Application.Contracts.Repositories
{
    public interface IEmailRepository
    {
        Task<IReadOnlyList<Email>> GetLatestAsync(string? search, CancellationToken ct = default);
        Task<Email?> GetAsync(Guid id, CancellationToken ct = default);
        Task<Email?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
        Task<Guid> AddAsync(Email entity, CancellationToken ct = default);
        Task UpdateAsync(Email entity, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

