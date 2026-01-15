using System;
using System.Threading.Tasks;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Core.Application.Contracts.Repositories
{
    public interface ISummaryRepository
    {
        Task<Summary?> GetAsync(Guid id, CancellationToken ct = default);
        Task<Summary?> GetByEmailIdAsync(Guid emailId, CancellationToken ct = default);
        Task<Summary?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
        Task<Summary?> GetForUpdateByEmailIdAsync(Guid emailId, CancellationToken ct = default);
        Task<Guid> AddAsync(Summary entity, CancellationToken ct = default);
        Task UpdateAsync(Summary entity, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

