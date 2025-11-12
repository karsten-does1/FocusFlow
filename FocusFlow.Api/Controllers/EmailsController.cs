using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/emails")]
    public sealed class EmailsController : ControllerBase
    {
        private readonly IEmailRepository _repo;   
        public EmailsController(IEmailRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<EmailDto>>> Get([FromQuery] string? q, CancellationToken ct)
            => Ok(await _repo.GetLatestAsync(q, ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<EmailDto>> GetById(Guid id, CancellationToken ct)
        {
            var item = await _repo.GetAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create([FromBody] EmailDto dto, CancellationToken ct)
        {
            var id = await _repo.AddAsync(dto, ct);
            return Ok(id);
        }
    }
}
