using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public sealed class TasksController : ControllerBase
    {
        private readonly ITaskService _service;
        public TasksController(ITaskService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<FocusTaskDto>>> Get([FromQuery] bool? done, CancellationToken ct)
            => Ok(await _service.ListAsync(done, ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FocusTaskDto>> GetById(Guid id, CancellationToken ct)
        {
            var item = await _service.GetAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create([FromBody] FocusTaskDto dto, CancellationToken ct)
            => Ok(await _service.AddAsync(dto, ct));

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] FocusTaskDto dto, CancellationToken ct)
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