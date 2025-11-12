using System;
using System.Threading.Tasks;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface ISummaryService
    {
        Task UpsertAsync(Guid emailId, string text, CancellationToken ct = default);
        Task<string?> GetTextAsync(Guid emailId, CancellationToken ct = default);
    }
}

