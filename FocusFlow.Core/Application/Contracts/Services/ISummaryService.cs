using System;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface ISummaryService
    {
        Task<SummaryDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<SummaryDto?> GetByEmailIdAsync(Guid emailId, CancellationToken ct = default);
        Task<Guid> AddAsync(SummaryDto dto, CancellationToken ct = default);
        Task UpdateAsync(SummaryDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

