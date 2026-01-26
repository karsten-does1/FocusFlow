using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs.Settings;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface ISettingsService
    {
        Task<AppSettingsDto> GetAsync(CancellationToken ct = default);
        Task<AppSettingsDto> UpdateAsync(AppSettingsDto dto, CancellationToken ct = default);
    }
}
