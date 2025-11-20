using System;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface IGmailSyncService
    {
        Task<EmailSyncResultDto> SyncLatestAsync(Guid emailAccountId, int maxCount = 20, CancellationToken ct = default);
    }
}