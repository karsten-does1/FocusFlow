using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusFlow.Infrastructure.Repositories
{
    public sealed class EmailAccountRepository : IEmailAccountRepository
    {
        private readonly FocusFlowDbContext _db;

        public EmailAccountRepository(FocusFlowDbContext db) => _db = db;

        public async Task<Guid> AddAsync(EmailAccount entity, CancellationToken ct = default)
        {
            _db.EmailAccounts.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<EmailAccount?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(account => account.Id == id, ct);
        }

        public async Task<EmailAccount?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.EmailAccounts
                .FirstOrDefaultAsync(account => account.Id == id, ct);
        }

        public async Task<EmailAccount?> GetByEmailAddressAsync(string emailAddress, CancellationToken ct = default)
        {
            return await _db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(account => account.EmailAddress == emailAddress, ct);
        }

        public async Task<IReadOnlyList<EmailAccount>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.EmailAccounts
                .AsNoTracking()
                .OrderBy(account => account.EmailAddress)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(EmailAccount entity, CancellationToken ct = default)
        {
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetForUpdateAsync(id, ct);
            if (entity is null) return;

            _db.EmailAccounts.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}

