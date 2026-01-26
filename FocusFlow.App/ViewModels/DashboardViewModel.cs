using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FocusFlow.App.Messages;
using FocusFlow.App.Services;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.DTOs.Settings;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel, IDisposable
    {
        private readonly ITaskService _taskService;
        private readonly IEmailService _emailService;
        private readonly IReminderService _reminderService;
        private readonly IBriefingService _briefingService;
        private readonly SettingsApi _settingsApi;

        private readonly ITextToSpeechService _tts;

        private bool _disposed;

        [ObservableProperty] private int _totalTasks;
        [ObservableProperty] private int _activeTasks;
        [ObservableProperty] private int _completedTasks;

        [ObservableProperty] private int _totalEmails;
        [ObservableProperty] private int _highPriorityEmails;
        [ObservableProperty] private int _upcomingReminders;

        [ObservableProperty] private ObservableCollection<EmailDto> _recentItems = new();

        [ObservableProperty] private DateTime? _briefingGeneratedAtUtc;
        [ObservableProperty] private ObservableCollection<EmailDto> _briefingImportantEmails = new();
        [ObservableProperty] private ObservableCollection<FocusTaskDto> _briefingDueTasks = new();
        [ObservableProperty] private ObservableCollection<ReminderDto> _briefingUpcomingReminders = new();

        [ObservableProperty] private int _briefingTasksHours = 48;
        [ObservableProperty] private int _briefingRemindersHours = 24;
        [ObservableProperty] private int _briefingEmailsDays = 2;

        [ObservableProperty] private string _tasksWindowText = "48 hours";
        [ObservableProperty] private string _remindersWindowText = "24 hours";

        public DateTime? BriefingGeneratedAtLocal => BriefingGeneratedAtUtc?.ToLocalTime();

        [ObservableProperty] private string _briefingSpeechMode = "Expanded";
        [ObservableProperty] private bool _isSpeaking;

        public DashboardViewModel(
            ITaskService taskService,
            IEmailService emailService,
            IReminderService reminderService,
            IBriefingService briefingService,
            SettingsApi settingsApi,
            ITextToSpeechService tts)
        {
            _taskService = taskService;
            _emailService = emailService;
            _reminderService = reminderService;
            _briefingService = briefingService;
            _settingsApi = settingsApi;
            _tts = tts;

            WeakReferenceMessenger.Default.Register<BriefingSettingsSavedMessage>(this, async (_, __) =>
            {
                await LoadBriefingAsync();
            });

            WeakReferenceMessenger.Default.Register<DailyBriefingDueMessage>(this, async (_, __) =>
            {
                await LoadBriefingAsync();
            });

            _ = LoadDashboardCommand.ExecuteAsync(null);
            _ = LoadBriefingCommand.ExecuteAsync(null);
        }

        partial void OnBriefingGeneratedAtUtcChanged(DateTime? value)
        {
            OnPropertyChanged(nameof(BriefingGeneratedAtLocal));
        }

        [RelayCommand]
        private async Task LoadDashboardAsync()
        {
            await ExecuteAsync(async () =>
            {
                var allTasks = await _taskService.ListAsync(null);
                TotalTasks = allTasks.Count;
                ActiveTasks = allTasks.Count(t => !t.IsDone);
                CompletedTasks = allTasks.Count(t => t.IsDone);

                var emails = await _emailService.GetLatestAsync(null);
                TotalEmails = emails.Count;
                HighPriorityEmails = emails.Count(e => e.PriorityScore >= 70);

                var untilDate = DateTime.UtcNow.AddDays(7);
                var reminders = await _reminderService.UpcomingAsync(untilDate);
                UpcomingReminders = reminders.Count;

                UpdateCollection(RecentItems, emails.Take(5));
            }, "Failed to load dashboard");
        }

        [RelayCommand]
        private async Task LoadBriefingAsync()
        {
            await ExecuteAsync(async () =>
            {
                AppSettingsDto settings = await _settingsApi.GetAsync();

                BriefingTasksHours = settings.Briefing.TasksHours;
                BriefingRemindersHours = settings.Briefing.RemindersHours;
                BriefingEmailsDays = settings.Briefing.EmailsDays;

                BriefingSpeechMode = string.IsNullOrWhiteSpace(settings.Briefing.SpeechMode)
                    ? "Expanded"
                    : settings.Briefing.SpeechMode.Trim();

                TasksWindowText = ToNiceDurationHours(BriefingTasksHours);
                RemindersWindowText = ToNiceDurationHours(BriefingRemindersHours);

                var briefing = await _briefingService.GetTodayAsync();

                BriefingGeneratedAtUtc = briefing.GeneratedAtUtc;
                UpdateCollection(BriefingImportantEmails, briefing.ImportantEmails);
                UpdateCollection(BriefingDueTasks, briefing.DueTasks);
                UpdateCollection(BriefingUpcomingReminders, briefing.UpcomingReminders);

            }, "Failed to load daily briefing");
        }

        [RelayCommand]
        private async Task SpeakBriefingAsync()
        {
            if (IsSpeaking) return;

            try
            {
                var mode = (BriefingSpeechMode ?? "Expanded").Trim();
                if (mode.Equals("Off", StringComparison.OrdinalIgnoreCase))
                    return;

                var important = BriefingImportantEmails?.Count ?? 0;
                var tasks = BriefingDueTasks?.Count ?? 0;
                var reminders = BriefingUpcomingReminders?.Count ?? 0;

                var text =
                    $"Daily briefing. You have {important} important emails, {tasks} tasks, and {reminders} reminders.";

                if (!mode.Equals("Simple", StringComparison.OrdinalIgnoreCase))
                {
                    var topMails = BriefingImportantEmails
                        .Take(3)
                        .Select(e => string.IsNullOrWhiteSpace(e.Subject) ? "(no subject)" : e.Subject.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    var topTasks = BriefingDueTasks
                        .Take(3)
                        .Select(t => string.IsNullOrWhiteSpace(t.Title) ? "(untitled task)" : t.Title.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    if (topMails.Count > 0)
                        text += " Top important emails are: " + string.Join(". ", topMails) + ".";

                    if (topTasks.Count > 0)
                        text += " Top tasks are: " + string.Join(". ", topTasks) + ".";
                }

                ErrorMessage = null;

                IsSpeaking = true;
                try
                {
                    await _tts.SpeakAsync(text);
                }
                finally
                {
                    IsSpeaking = false;
                }
            }
            catch (OperationCanceledException)
            {
                IsSpeaking = false;
            }
            catch
            {
                ErrorMessage = "Failed to speak briefing";
                IsSpeaking = false;
            }
        }


        [RelayCommand]
        private void StopSpeaking()
        {
            _tts.Stop();
            IsSpeaking = false;
        }

        private static string ToNiceDurationHours(int hours)
        {
            if (hours < 24)
                return hours == 1 ? "1 hour" : $"{hours} hours";

            var days = hours / 24;
            if (hours % 24 == 0)
                return days == 1 ? "1 day" : $"{days} days";

            return $"{hours} hours";
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}
