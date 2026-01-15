using FocusFlow.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface IOutlookSyncService
    {
        Task<EmailSyncResultDto> SyncLatestAsync(Guid emailAccountId, int maxCount = 20, CancellationToken ct = default);
    }
}
