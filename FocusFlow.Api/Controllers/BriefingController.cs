using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.DTOs.Settings;
using FocusFlow.Core.Application.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/briefing")]
    public sealed class BriefingController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ITaskService _taskService;
        private readonly IReminderService _reminderService;
        private readonly ISettingsService _settingsService;

        public BriefingController(
            IEmailService emailService,
            ITaskService taskService,
            IReminderService reminderService,
            ISettingsService settingsService)
        {
            _emailService = emailService;
            _taskService = taskService;
            _reminderService = reminderService;
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<ActionResult<DailyBriefingDto>> Get(CancellationToken ct)
        {
            AppSettingsDto settings = await _settingsService.GetAsync(ct);

            var now = DateTime.UtcNow;
            var taskUntil = now.AddHours(settings.Briefing.TasksHours);
            var reminderUntil = now.AddHours(settings.Briefing.RemindersHours);

            var emails = await _emailService.GetLatestAsync(null, ct);

            var importantEmails = emails
                .Where(e =>
                    e.ReceivedUtc >= now.AddDays(-settings.Briefing.EmailsDays) &&
                    (e.PriorityScore >= 70 ||
                     !string.IsNullOrWhiteSpace(e.SuggestedAction) ||
                     !string.IsNullOrWhiteSpace(e.Category)))
                .OrderByDescending(e => e.PriorityScore)
                .ThenByDescending(e => e.ReceivedUtc)
                .Take(5)
                .ToList();

            var tasks = await _taskService.ListAsync(done: false, ct);

            var dueTasks = tasks
                .Where(t =>
                    !t.IsDone &&
                    t.DueUtc is not null &&
                    t.DueUtc.Value >= now &&
                    t.DueUtc.Value <= taskUntil)
                .OrderBy(t => t.DueUtc)
                .Take(10)
                .ToList();

            var reminders = await _reminderService.UpcomingAsync(reminderUntil, ct);

            var upcomingReminders = reminders
                .Where(r =>
                    !r.Fired &&
                    r.FireAtUtc >= now &&
                    r.FireAtUtc <= reminderUntil)
                .OrderBy(r => r.FireAtUtc)
                .Take(10)
                .ToList();

            var dto = new DailyBriefingDto(
                GeneratedAtUtc: now,
                ImportantEmails: importantEmails,
                DueTasks: dueTasks,
                UpcomingReminders: upcomingReminders);

            return Ok(dto);
        }
    }
}
