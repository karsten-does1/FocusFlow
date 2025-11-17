using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusFlow.Infrastructure.Repositories
{
    public sealed class EmailRepository : IEmailRepository
    {
        private readonly FocusFlowDbContext _db;

        public EmailRepository(FocusFlowDbContext db) => _db = db;

        public async Task<Guid> AddAsync(Email entity, CancellationToken ct = default)
        {
            _db.Emails.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<Email?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Emails
                .AsNoTracking()
                .FirstOrDefaultAsync(email => email.Id == id, ct);
        }

        public async Task<Email?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Emails
                .FirstOrDefaultAsync(email => email.Id == id, ct);
        }

        public async Task<IReadOnlyList<Email>> GetLatestAsync(string? search, CancellationToken ct = default)
        {
            var query = _db.Emails.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();
                query = query.Where(email =>
                    EF.Functions.Like(email.From, $"%{searchTerm}%") ||
                    EF.Functions.Like(email.Subject, $"%{searchTerm}%") ||
                    EF.Functions.Like(email.BodyText, $"%{searchTerm}%"));
            }

            return await query
                .OrderByDescending(email => email.ReceivedUtc)
                .Take(100)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(Email entity, CancellationToken ct = default)
        {
            
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetForUpdateAsync(id, ct);
            if (entity is null) return;

            _db.Emails.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
