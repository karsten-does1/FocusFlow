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

        public async Task<string?> GetTextAsync(Guid emailId, CancellationToken ct = default)
        {
            return await _db.Summaries
                .AsNoTracking()
                .Where(s => s.EmailId == emailId)
                .OrderByDescending(s => s.CreatedUtc)
                .Select(s => s.Text)
                .FirstOrDefaultAsync(ct);
        }

        public async Task UpsertAsync(Guid emailId, string text, CancellationToken ct = default)
        {
            var entity = await _db.Summaries.FirstOrDefaultAsync(s => s.EmailId == emailId, ct);

            if (entity is null)
            {
                _db.Summaries.Add(new Summary(emailId, text));
            }
            else
            {
                entity.UpdateText(text);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}