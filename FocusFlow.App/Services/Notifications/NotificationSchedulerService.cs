using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FocusFlow.App.Messages;
using FocusFlow.App.Services;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services.Notifications
{
    public sealed class NotificationSchedulerService : BackgroundService
    {
        private readonly SettingsApi _settingsApi;
        private readonly IReminderService _reminderService;
        private readonly IBriefingService _briefingService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationSchedulerService> _logger;

        private readonly SemaphoreSlim _wakeSignal = new(0, 1);

        private DateOnly? _lastBriefingDateLocal;
        private DateTime _lastSettingsFetchUtc = DateTime.MinValue;

        private bool _enabled = true;
        private int _tickSeconds = 60;
        private int _upcomingMinutes = 5;

        private bool _briefingEnabled = true;
        private TimeSpan _briefingTimeLocal = TimeSpan.FromHours(9);

        private string _lastBriefingTimeLocalString = "09:00";

        public NotificationSchedulerService(
            SettingsApi settingsApi,
            IReminderService reminderService,
            IBriefingService briefingService,
            INotificationService notificationService,
            ILogger<NotificationSchedulerService> logger)
        {
            _settingsApi = settingsApi;
            _reminderService = reminderService;
            _briefingService = briefingService;
            _notificationService = notificationService;
            _logger = logger;

            WeakReferenceMessenger.Default.Register<NotificationSettingsSavedMessage>(this, (_, __) =>
            {
                _lastSettingsFetchUtc = DateTime.MinValue;

                if (_wakeSignal.CurrentCount == 0)
                    _wakeSignal.Release();
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshSettingsIfNeededAsync(stoppingToken);

                    if (_enabled)
                    {
                        await CheckRemindersAsync(stoppingToken);
                        await CheckBriefingAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "NotificationScheduler tick failed");
                }

                var tick = TimeSpan.FromSeconds(Math.Clamp(_tickSeconds, 5, 3600));

                try
                {
                    await _wakeSignal.WaitAsync(tick, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RefreshSettingsIfNeededAsync(CancellationToken ct)
        {
            if (DateTime.UtcNow - _lastSettingsFetchUtc < TimeSpan.FromSeconds(30))
                return;

            try
            {
                var settings = await _settingsApi.GetAsync(ct);

                _lastSettingsFetchUtc = DateTime.UtcNow;

                _enabled = settings.Notifications.Enabled;
                _tickSeconds = Math.Clamp(settings.Notifications.TickSeconds, 5, 3600);
                _upcomingMinutes = Math.Clamp(settings.Notifications.ReminderUpcomingWindowMinutes, 1, 240);

                _briefingEnabled = settings.Notifications.BriefingEnabled;

                var newTimeString = (settings.Notifications.BriefingTimeLocal ?? "09:00").Trim();

                var parsedOk = TryParseHhMm(newTimeString, out var newTime);
                _briefingTimeLocal = parsedOk ? newTime : TimeSpan.FromHours(9);

                if (!string.Equals(newTimeString, _lastBriefingTimeLocalString, StringComparison.Ordinal))
                {
                    var nowLocal = DateTime.Now;
                    var newTriggerLocal = nowLocal.Date.Add(_briefingTimeLocal);

                    if (nowLocal < newTriggerLocal)
                    {
                        _lastBriefingDateLocal = null;
                        _logger.LogInformation(
                            "Briefing time changed to {Time}; reset last briefing date so it can fire today.",
                            newTimeString);
                    }

                    _lastBriefingTimeLocalString = newTimeString;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh notification settings; keeping cached values.");
            }
        }

        private static bool TryParseHhMm(string hhmm, out TimeSpan time)
        {
            time = TimeSpan.FromHours(9);

            if (string.IsNullOrWhiteSpace(hhmm))
                return false;

            if (TimeOnly.TryParseExact(hhmm, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            {
                time = t.ToTimeSpan();
                return true;
            }

            return false;
        }

        private async Task CheckRemindersAsync(CancellationToken ct)
        {
            var untilUtc = DateTime.UtcNow.AddMinutes(_upcomingMinutes);

            var upcoming = await _reminderService.UpcomingAsync(untilUtc, ct);
            if (upcoming.Count == 0) return;

            var due = upcoming
                .Where(r => !r.Fired && r.FireAtUtc <= untilUtc)
                .OrderBy(r => r.FireAtUtc)
                .ToList();

            foreach (var reminder in due)
            {
                var when = reminder.FireAtUtc.ToLocalTime().ToString("HH:mm");
                _notificationService.Show("Reminder", $"{reminder.Title} (om {when})");

                try
                {
                    await _reminderService.MarkFiredAsync(reminder.Id, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to mark reminder as fired (Id={ReminderId})", reminder.Id);
                }
            }
        }

        private async Task CheckBriefingAsync(CancellationToken ct)
        {
            if (!_briefingEnabled) return;

            var nowLocal = DateTime.Now;
            var todayLocal = DateOnly.FromDateTime(nowLocal);

            if (_lastBriefingDateLocal == todayLocal) return;

            var triggerLocal = nowLocal.Date.Add(_briefingTimeLocal);
            if (nowLocal < triggerLocal) return;

            var briefing = await _briefingService.GetTodayAsync(ct);

            var important = briefing.ImportantEmails?.Count ?? 0;
            var dueTasks = briefing.DueTasks?.Count ?? 0;
            var upcomingReminders = briefing.UpcomingReminders?.Count ?? 0;

            _notificationService.Show(
                "Daily briefing",
                $"Belangrijk: {important} • Taken: {dueTasks} • Reminders: {upcomingReminders}");

            _lastBriefingDateLocal = todayLocal;

            WeakReferenceMessenger.Default.Send(DailyBriefingDueMessage.Instance);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            return base.StopAsync(cancellationToken);
        }
    }
}
