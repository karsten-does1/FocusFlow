using FocusFlow.Core.Application.Contracts.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/summaries")]
    public sealed class SummariesController : ControllerBase
    {
        private readonly ISummaryRepository _repo;
        public SummariesController(ISummaryRepository repo) => _repo = repo;

        [HttpGet("{emailId:guid}")]
        public async Task<ActionResult<string?>> Get(Guid emailId, CancellationToken ct)
            => Ok(await _repo.GetTextAsync(emailId, ct));

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] SummaryRequest req, CancellationToken ct)
        {
            await _repo.UpsertAsync(req.EmailId, req.Text, ct);
            return NoContent();
        }

        public sealed record SummaryRequest(Guid EmailId, string Text);
    }
}
