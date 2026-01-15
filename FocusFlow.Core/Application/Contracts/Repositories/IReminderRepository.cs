using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Core.Application.Contracts.Repositories
{
    public interface IReminderRepository
    {
        Task<Guid> AddAsync(Reminder entity, CancellationToken ct = default);
        Task<Reminder?> GetAsync(Guid id, CancellationToken ct = default);
        Task<Reminder?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Reminder>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Reminder>> UpcomingAsync(DateTime untilUtc, CancellationToken ct = default);
        Task MarkFiredAsync(Guid id, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

