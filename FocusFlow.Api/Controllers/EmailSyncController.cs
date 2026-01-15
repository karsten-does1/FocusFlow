using System;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/email-sync")]
    public sealed class EmailSyncController : ControllerBase
    {
        private const int DefaultMaxCount = 20;
        private const int MaxMaxCount = 500;
        private const int MinMaxCount = 1;

        private readonly IGmailSyncService _gmailSync;
        private readonly IOutlookSyncService _outlookSync;

        public EmailSyncController(
            IGmailSyncService gmailSync,
            IOutlookSyncService outlookSync)
        {
            _gmailSync = gmailSync;
            _outlookSync = outlookSync;
        }

        [HttpPost("gmail/{accountId:guid}")]
        public async Task<ActionResult<EmailSyncResultDto>> SyncGmail(
            Guid accountId,
            [FromQuery] int maxCount = DefaultMaxCount,
            CancellationToken ct = default)
        {
            if (maxCount < MinMaxCount || maxCount > MaxMaxCount)
            {
                return BadRequest(new
                {
                    error = "maxCount must be between 1 and 500",
                    min = MinMaxCount,
                    max = MaxMaxCount,
                    provided = maxCount
                });
            }

            try
            {
                var result = await _gmailSync.SyncLatestAsync(accountId, maxCount, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred during Gmail sync",
                    message = ex.Message
                });
            }
        }

        [HttpPost("microsoft/{accountId:guid}")]
        public async Task<ActionResult<EmailSyncResultDto>> SyncMicrosoft(
            Guid accountId,
            [FromQuery] int maxCount = DefaultMaxCount,
            CancellationToken ct = default)
        {
            if (maxCount < MinMaxCount || maxCount > MaxMaxCount)
            {
                return BadRequest(new
                {
                    error = "maxCount must be between 1 and 500",
                    min = MinMaxCount,
                    max = MaxMaxCount,
                    provided = maxCount
                });
            }

            try
            {
                var result = await _outlookSync.SyncLatestAsync(accountId, maxCount, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred during Microsoft/Outlook sync",
                    message = ex.Message
                });
            }
        }
    }
}