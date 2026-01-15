using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/summaries")]
    public sealed class SummariesController : ControllerBase
    {
        private readonly ISummaryService _service;
        public SummariesController(ISummaryService service) => _service = service;

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SummaryDto>> GetById(Guid id, CancellationToken ct)
        {
            var item = await _service.GetAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpGet("email/{emailId:guid}")]
        public async Task<ActionResult<SummaryDto>> GetByEmailId(Guid emailId, CancellationToken ct)
        {
            var item = await _service.GetByEmailIdAsync(emailId, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create([FromBody] SummaryDto dto, CancellationToken ct)
        {
            var id = await _service.AddAsync(dto, ct);
            return Ok(id);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SummaryDto dto, CancellationToken ct)
        {
            if (id != dto.Id) return BadRequest();
            await _service.UpdateAsync(dto, ct);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
