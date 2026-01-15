using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusFlow.Infrastructure.Repositories
{
    public sealed class TaskRepository : ITaskRepository
    {
        private readonly FocusFlowDbContext _db;

        public TaskRepository(FocusFlowDbContext db) => _db = db;

        public async Task<Guid> AddAsync(FocusTask entity, CancellationToken ct = default)
        {
            _db.Tasks.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<FocusTask?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(task => task.Id == id, ct);
        }

        public async Task<FocusTask?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Tasks
                .FirstOrDefaultAsync(task => task.Id == id, ct);
        }

        public async Task<IReadOnlyList<FocusTask>> ListAsync(bool? done = null, CancellationToken ct = default)
        {
            var query = _db.Tasks.AsNoTracking();

            if (done is not null)
            {
                query = query.Where(task => task.IsDone == done);
            }

            return await query
                .OrderBy(task => task.IsDone)
                .ThenBy(task => task.DueUtc ?? DateTime.MaxValue)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(FocusTask entity, CancellationToken ct = default)
        {
          await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetForUpdateAsync(id, ct);
            if (entity is null) return;

            _db.Tasks.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
