using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusFlow.Infrastructure.Repositories
{
    public sealed class ReminderRepository : IReminderRepository
    {
        private readonly FocusFlowDbContext _db;

        public ReminderRepository(FocusFlowDbContext db) => _db = db;

        public async Task<Guid> AddAsync(Reminder entity, CancellationToken ct = default)
        {
            _db.Reminders.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<Reminder?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Reminders
                .AsNoTracking()
                .FirstOrDefaultAsync(reminder => reminder.Id == id, ct);
        }

        public async Task<Reminder?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Reminders
                .FirstOrDefaultAsync(reminder => reminder.Id == id, ct);
        }

        public async Task<IReadOnlyList<Reminder>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Reminders
                .AsNoTracking()
                .OrderBy(reminder => reminder.FireAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Reminder>> UpcomingAsync(DateTime untilUtc, CancellationToken ct = default)
        {
            return await _db.Reminders
                .AsNoTracking()
                .Where(reminder => !reminder.Fired && reminder.FireAtUtc <= untilUtc)
                .OrderBy(reminder => reminder.FireAtUtc)
                .ToListAsync(ct);
        }

        public async Task MarkFiredAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetForUpdateAsync(id, ct);
            if (entity is null) return;

            entity.MarkFired();
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetForUpdateAsync(id, ct);
            if (entity is null) return;

            _db.Reminders.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}

