using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs.Settings;
using FocusFlow.Core.Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public sealed class SettingsController : ControllerBase
    {
        private readonly ISettingsService _service;

        public SettingsController(ISettingsService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<AppSettingsDto>> Get(CancellationToken ct)
            => Ok(await _service.GetAsync(ct));

        [HttpPost]
        public async Task<ActionResult<AppSettingsDto>> Update([FromBody] AppSettingsDto dto, CancellationToken ct)
            => Ok(await _service.UpdateAsync(dto, ct));
    }
}
