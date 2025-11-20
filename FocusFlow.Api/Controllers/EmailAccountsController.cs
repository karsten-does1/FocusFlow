using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/emailaccounts")]
    public sealed class EmailAccountsController : ControllerBase
    {
        private readonly IEmailAccountService _service;
        public EmailAccountsController(IEmailAccountService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<EmailAccountDto>>> Get(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<EmailAccountDto>> GetById(Guid id, CancellationToken ct)
        {
            var item = await _service.GetAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpGet("by-email/{emailAddress}")]
        public async Task<ActionResult<EmailAccountDto>> GetByEmailAddress(string emailAddress, CancellationToken ct)
        {
            var item = await _service.GetByEmailAddressAsync(emailAddress, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create([FromBody] EmailAccountDto dto, CancellationToken ct)
        {
            var id = await _service.AddAsync(dto, ct);
            return Ok(id);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EmailAccountDto dto, CancellationToken ct)
        {
            if (id != dto.Id) return BadRequest();
            await _service.UpdateAsync(dto, ct);
            return NoContent();
        }

        [HttpPut("{id:guid}/tokens")]
        public async Task<IActionResult> UpdateTokens(
            Guid id,
            [FromBody] UpdateTokensRequest request,
            CancellationToken ct)
        {
            await _service.UpdateTokensAsync(id, request.AccessToken, request.ExpiresAtUtc, request.RefreshToken, ct);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
    }

    public sealed record UpdateTokensRequest(
        string AccessToken,
        DateTime ExpiresAtUtc,
        string? RefreshToken = null);
    
    public sealed class CreateEmailAccountRequest
    {
        public EmailProvider Provider { get; set; }
        public string EmailAddress { get; set; } = "";
        public string? DisplayName { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}

