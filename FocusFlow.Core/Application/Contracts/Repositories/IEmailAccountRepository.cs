using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusFlow.Core.Domain.Entities;

namespace FocusFlow.Core.Application.Contracts.Repositories
{
    public interface IEmailAccountRepository
    {
        Task<Guid> AddAsync(EmailAccount entity, CancellationToken ct = default);
        Task UpdateAsync(EmailAccount entity, CancellationToken ct = default);
        Task<EmailAccount?> GetAsync(Guid id, CancellationToken ct = default);
        Task<EmailAccount?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
        Task<EmailAccount?> GetByEmailAddressAsync(string emailAddress, CancellationToken ct = default);
        Task<IReadOnlyList<EmailAccount>> GetAllAsync(CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

