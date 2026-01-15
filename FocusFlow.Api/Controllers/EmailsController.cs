using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/emails")]
    public sealed class EmailsController : ControllerBase
    {
        private readonly IEmailService _service;   
        public EmailsController(IEmailService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<EmailDto>>> Get([FromQuery] string? q, CancellationToken ct)
            => Ok(await _service.GetLatestAsync(q, ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<EmailDto>> GetById(Guid id, CancellationToken ct)
        {
            var item = await _service.GetAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create([FromBody] EmailDto dto, CancellationToken ct)
        {
            var id = await _service.AddAsync(dto, ct);
            return Ok(id);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EmailDto dto, CancellationToken ct)
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

        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAll(CancellationToken ct)
        {
            await _service.DeleteAllAsync(ct);
            return NoContent();
        }
    }
}
