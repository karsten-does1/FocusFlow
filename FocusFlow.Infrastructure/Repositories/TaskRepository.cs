using FocusFlow.Core.Application.Contracts.DTOs;
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

        public async Task<Guid> AddAsync(FocusTaskDto dto, CancellationToken ct = default)
        {
            var entity = new FocusTask(dto.Title, dto.Notes, dto.DueUtc, dto.RelatedEmailId);
            if (dto.IsDone) entity.Complete();

            _db.Tasks.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<FocusTaskDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<IReadOnlyList<FocusTaskDto>> ListAsync(bool? done = null, CancellationToken ct = default)
        {
            var query = _db.Tasks.AsNoTracking();

            if (done is not null)
            {
                query = query.Where(t => t.IsDone == done);
            }

            return await query
                .OrderBy(t => t.IsDone)
                .ThenBy(t => t.DueUtc ?? DateTime.MaxValue)
                .Select(t => MapToDto(t))
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(FocusTaskDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null) return;

            entity.Update(dto.Title, dto.Notes, dto.DueUtc);
            if (dto.IsDone) entity.Complete();
            else entity.Reopen();

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return;

            _db.Tasks.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }

        private static FocusTaskDto MapToDto(FocusTask entity) =>
            new FocusTaskDto(entity.Id, entity.Title, entity.Notes, entity.DueUtc, entity.IsDone, entity.RelatedEmailId);
    }
}
