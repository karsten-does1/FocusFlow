using FocusFlow.Core.Application.Contracts.DTOs;
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

        public async Task<Guid> AddAsync(EmailDto dto, CancellationToken ct = default)
        {
            var entity = new Email(dto.From, dto.Subject, dto.BodyText, dto.ReceivedUtc);
            entity.SetPriority(dto.PriorityScore);

            _db.Emails.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<EmailDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Emails
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<IReadOnlyList<EmailDto>> GetLatestAsync(string? search, CancellationToken ct = default)
        {
            var query = _db.Emails.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();
                query = query.Where(e =>
                    EF.Functions.Like(e.From, $"%{searchTerm}%") ||
                    EF.Functions.Like(e.Subject, $"%{searchTerm}%") ||
                    EF.Functions.Like(e.BodyText, $"%{searchTerm}%"));
            }

            return await query
                .OrderByDescending(e => e.ReceivedUtc)
                .Take(100)
                .Select(e => MapToDto(e))
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(EmailDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Emails.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity is null) return;

            entity.SetPriority(dto.PriorityScore);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Emails.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return;

            _db.Emails.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }

        private static EmailDto MapToDto(Email entity) =>
            new EmailDto(entity.Id, entity.From, entity.Subject, entity.BodyText, entity.ReceivedUtc, entity.PriorityScore);
    }
}
