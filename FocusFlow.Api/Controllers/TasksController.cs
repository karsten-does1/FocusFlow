using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public sealed class TasksController : ControllerBase
    {
        private readonly ITaskRepository _repo;
        public TasksController(ITaskRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<FocusTaskDto>>> Get([FromQuery] bool? done, CancellationToken ct)
            => Ok(await _repo.ListAsync(done, ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FocusTaskDto>> GetById(Guid id, CancellationToken ct)
        {
            var item = await _repo.GetAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create([FromBody] FocusTaskDto dto, CancellationToken ct)
            => Ok(await _repo.AddAsync(dto, ct));

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] FocusTaskDto dto, CancellationToken ct)
        {
            if (id != dto.Id) return BadRequest();
            await _repo.UpdateAsync(dto, ct);
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