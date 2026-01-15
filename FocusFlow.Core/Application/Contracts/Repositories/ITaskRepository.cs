using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Core.Application.Contracts.Repositories
{
    public interface ITaskRepository
    {
        Task<Guid> AddAsync(FocusTask entity, CancellationToken ct = default);
        Task UpdateAsync(FocusTask entity, CancellationToken ct = default);
        Task<FocusTask?> GetAsync(Guid id, CancellationToken ct = default);
        Task<FocusTask?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<FocusTask>> ListAsync(bool? done = null, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

