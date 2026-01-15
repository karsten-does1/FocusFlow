using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusFlow.Infrastructure.Repositories
{
    public sealed class SummaryRepository : ISummaryRepository
    {
        private readonly FocusFlowDbContext _db;

        public SummaryRepository(FocusFlowDbContext db) => _db = db;

        public async Task<Summary?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Summaries
                .AsNoTracking()
                .FirstOrDefaultAsync(summary => summary.Id == id, ct);
        }

        public async Task<Summary?> GetByEmailIdAsync(Guid emailId, CancellationToken ct = default)
        {
            return await _db.Summaries
                .AsNoTracking()
                .Where(summary => summary.EmailId == emailId)
                .OrderByDescending(summary => summary.CreatedUtc)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Summary?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Summaries
                .FirstOrDefaultAsync(summary => summary.Id == id, ct);
        }

        public async Task<Summary?> GetForUpdateByEmailIdAsync(Guid emailId, CancellationToken ct = default)
        {
            return await _db.Summaries
                .Where(summary => summary.EmailId == emailId)
                .OrderByDescending(summary => summary.CreatedUtc)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Guid> AddAsync(Summary entity, CancellationToken ct = default)
        {
            _db.Summaries.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateAsync(Summary entity, CancellationToken ct = default)
        {
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetForUpdateAsync(id, ct);
            if (entity is null) return;

            _db.Summaries.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}