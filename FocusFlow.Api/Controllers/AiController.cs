using System.Threading;
using System.Threading.Tasks;

using FocusFlow.Core.Application.Contracts.DTOs.Ai;
using FocusFlow.Core.Application.Contracts.Services;

using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;

        public AiController(IAiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("email/analyze")]
        public async Task<ActionResult<AnalyzeResponseDto>> Analyze([FromBody] AnalyzeRequestDto request, CancellationToken ct)
        {
            if (request is null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Subject))
                return BadRequest("Subject is required.");

            if (string.IsNullOrWhiteSpace(request.Body))
                return BadRequest("Body is required.");

            var result = await _aiService.AnalyzeEmailAsync(request, ct);

            return Ok(new AnalyzeResponseDto(
                Summary: result.Summary,
                PriorityScore: result.Priority,
                Category: result.Category,
                SuggestedAction: result.Action
            ));
        }

        [HttpPost("email/draft-reply")]
        public async Task<ActionResult<DraftReplyResponseDto>> DraftReply([FromBody] DraftReplyRequestDto request, CancellationToken ct)
        {
            if (request is null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Subject))
                return BadRequest("Subject is required.");

            if (string.IsNullOrWhiteSpace(request.Body))
                return BadRequest("Body is required.");

            var result = await _aiService.DraftReplyAsync(request, ct);
            return Ok(result);
        }

        [HttpPost("email/compose")]
        public async Task<ActionResult<ComposeEmailResponseDto>> Compose([FromBody] ComposeEmailRequestDto request, CancellationToken ct)
        {
            if (request is null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt is required.");

            var result = await _aiService.ComposeEmailAsync(request, ct);
            return Ok(result);
        }

        [HttpPost("email/extract-tasks")]
        public async Task<ActionResult<ExtractTasksResponseDto>> ExtractTasks([FromBody] ExtractTasksRequestDto request, CancellationToken ct)
        {
            if (request is null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Subject))
                return BadRequest("Subject is required.");

            if (string.IsNullOrWhiteSpace(request.Body))
                return BadRequest("Body is required.");

            var result = await _aiService.ExtractTasksAsync(request, ct);
            return Ok(result);
        }
    }
}
