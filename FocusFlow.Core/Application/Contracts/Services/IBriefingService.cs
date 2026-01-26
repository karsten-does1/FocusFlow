using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface IBriefingService
    {
        Task<DailyBriefingDto> GetTodayAsync(CancellationToken ct = default);
    }
}
