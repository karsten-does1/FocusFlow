using FocusFlow.Core.Application.Contracts.DTOs;
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

        public async Task<Guid> AddAsync(ReminderDto dto, CancellationToken ct = default)
        {
            var entity = new Reminder(dto.Title, dto.FireAtUtc, dto.RelatedTaskId, dto.RelatedEmailId);
            if (dto.Fired) entity.MarkFired();

            _db.Reminders.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<ReminderDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Reminders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<IReadOnlyList<ReminderDto>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Reminders
                .AsNoTracking()
                .OrderBy(r => r.FireAtUtc)
                .Select(r => MapToDto(r))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<ReminderDto>> UpcomingAsync(DateTime untilUtc, CancellationToken ct = default)
        {
            return await _db.Reminders
                .AsNoTracking()
                .Where(r => !r.Fired && r.FireAtUtc <= untilUtc)
                .OrderBy(r => r.FireAtUtc)
                .Select(r => MapToDto(r))
                .ToListAsync(ct);
        }

        public async Task MarkFiredAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Reminders.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return;

            entity.MarkFired();
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Reminders.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return;

            _db.Reminders.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }

        private static ReminderDto MapToDto(Reminder entity) =>
            new ReminderDto(entity.Id, entity.Title, entity.FireAtUtc, entity.Fired, entity.RelatedTaskId, entity.RelatedEmailId);
    }
}

