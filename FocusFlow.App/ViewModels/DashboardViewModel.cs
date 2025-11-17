using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FocusFlow.App.Messages;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly ITaskService _taskService;
        private readonly IEmailService _emailService;
        private readonly IReminderService _reminderService;

        #region Properties
        [ObservableProperty]
        private int _totalTasks;

        [ObservableProperty]
        private int _activeTasks;

        [ObservableProperty]
        private int _completedTasks;

        [ObservableProperty]
        private int _totalEmails;

        [ObservableProperty]
        private int _highPriorityEmails;

        [ObservableProperty]
        private int _upcomingReminders;

        [ObservableProperty]
        private ObservableCollection<object> _recentItems = new();
        #endregion

        public DashboardViewModel(
            ITaskService taskService,
            IEmailService emailService,
            IReminderService reminderService)
        {
            _taskService = taskService;
            _emailService = emailService;
            _reminderService = reminderService;

           
            WeakReferenceMessenger.Default.Register<TaskChangedMessage>(this, (receiver, message) => LoadDashboardCommand.ExecuteAsync(null));
            WeakReferenceMessenger.Default.Register<EmailChangedMessage>(this, (receiver, message) => LoadDashboardCommand.ExecuteAsync(null));
            WeakReferenceMessenger.Default.Register<ReminderChangedMessage>(this, (receiver, message) => LoadDashboardCommand.ExecuteAsync(null));
        }

        [RelayCommand]
        private async Task LoadDashboardAsync()
        {
            await ExecuteAsync(async () =>
            {
                
                var allTasks = await _taskService.ListAsync(null);
                TotalTasks = allTasks.Count;
                ActiveTasks = allTasks.Count(task => !task.IsDone);
                CompletedTasks = allTasks.Count(task => task.IsDone);

                
                var emails = await _emailService.GetLatestAsync(null);
                TotalEmails = emails.Count;
                HighPriorityEmails = emails.Count(email => email.PriorityScore >= 70);

                
                var untilDate = DateTime.UtcNow.AddDays(7);
                var reminders = await _reminderService.UpcomingAsync(untilDate);
                UpcomingReminders = reminders.Count;

               
                UpdateCollection(RecentItems, emails.Take(5));
            }, "Failed to load dashboard");
        }
    }
}

