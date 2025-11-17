using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/reminders")]
    public sealed class RemindersController : ControllerBase
    {
        private readonly IReminderRepository _repo;
        public RemindersController(IReminderRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ReminderDto>>> GetAll(CancellationToken ct)
            => Ok(await _repo.GetAllAsync(ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ReminderDto>> Get(Guid id, CancellationToken ct)
        {
            var item = await _repo.GetAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<IReadOnlyList<ReminderDto>>> GetUpcoming([FromQuery] DateTime untilUtc, CancellationToken ct)
            => Ok(await _repo.UpcomingAsync(untilUtc, ct));

        [HttpPost]
        public async Task<ActionResult<Guid>> Create([FromBody] ReminderDto dto, CancellationToken ct)
            => Ok(await _repo.AddAsync(dto, ct));

        [HttpPost("{id:guid}/fired")]
        public async Task<IActionResult> MarkFired(Guid id, CancellationToken ct)
        {
            await _repo.MarkFiredAsync(id, ct);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _repo.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
