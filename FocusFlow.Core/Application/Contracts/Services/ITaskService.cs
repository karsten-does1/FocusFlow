using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface ITaskService
    {
        Task<Guid> AddAsync(FocusTaskDto dto, CancellationToken ct = default);
        Task UpdateAsync(FocusTaskDto dto, CancellationToken ct = default);
        Task<FocusTaskDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<FocusTaskDto>> ListAsync(bool? done = null, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

