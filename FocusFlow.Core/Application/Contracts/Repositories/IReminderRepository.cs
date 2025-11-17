using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.Core.Application.Contracts.Repositories
{
    public interface IReminderRepository
    {
        Task<Guid> AddAsync(ReminderDto dto, CancellationToken ct = default);
        Task<ReminderDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<ReminderDto>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<ReminderDto>> UpcomingAsync(DateTime untilUtc, CancellationToken ct = default);
        Task MarkFiredAsync(Guid id, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

