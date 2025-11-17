using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.App.Messages;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.ViewModels
{
    public partial class RemindersViewModel : BaseViewModel
    {
        private readonly IReminderService _reminderService;

        #region Properties
        [ObservableProperty]
        private ObservableCollection<ReminderDto> _reminders = new();

        [ObservableProperty]
        private bool _showOnlyUpcoming = true;

        [ObservableProperty]
        private bool _isReminderFormVisible;

        [ObservableProperty]
        private string _reminderTitle = string.Empty;

        [ObservableProperty]
        private DateTime _reminderFireDate = DateTime.UtcNow.AddHours(1);

        [ObservableProperty]
        private Guid? _relatedTaskId;

        [ObservableProperty]
        private Guid? _relatedEmailId;
        #endregion

        public RemindersViewModel(IReminderService reminderService)
        {
            _reminderService = reminderService;
        }

        #region Commands
        [RelayCommand]
        private async Task LoadRemindersAsync()
        {
            await ExecuteAsync(async () =>
            {
                var untilDate = ShowOnlyUpcoming 
                    ? DateTime.UtcNow.AddDays(30) 
                    : DateTime.MaxValue;
                
                var reminders = await _reminderService.UpcomingAsync(untilDate);
                UpdateCollection(Reminders, reminders);
            }, "Failed to load reminders");
        }

        [RelayCommand]
        private void ShowNewReminderForm()
        {
            IsReminderFormVisible = true;
            ReminderTitle = string.Empty;
            ReminderFireDate = DateTime.UtcNow.AddHours(1);
            RelatedTaskId = null;
            RelatedEmailId = null;
        }

        [RelayCommand]
        private void CancelReminderForm()
        {
            IsReminderFormVisible = false;
        }

        [RelayCommand]
        private async Task SaveReminderAsync()
        {
            if (string.IsNullOrWhiteSpace(ReminderTitle))
            {
                ErrorMessage = "Reminder title is required";
                return;
            }

            await ExecuteAsync(async () =>
            {
                var reminder = new ReminderDto(
                    Guid.NewGuid(),
                    ReminderTitle,
                    ReminderFireDate,
                    false,
                    RelatedTaskId,
                    RelatedEmailId);

                await _reminderService.AddAsync(reminder);
                IsReminderFormVisible = false;
                await LoadRemindersAsync();
                NotifyChanged<ReminderChangedMessage>();
            }, "Failed to save reminder");
        }

        [RelayCommand]
        private async Task MarkFiredAsync(ReminderDto reminder)
        {
            await ExecuteAsync(async () =>
            {
                await _reminderService.MarkFiredAsync(reminder.Id);
                await LoadRemindersAsync();
                NotifyChanged<ReminderChangedMessage>();
            }, "Failed to mark reminder as fired");
        }

        [RelayCommand]
        private async Task DeleteReminderAsync(ReminderDto reminder)
        {
            await ExecuteAsync(async () =>
            {
                await _reminderService.DeleteAsync(reminder.Id);
                await LoadRemindersAsync();
                NotifyChanged<ReminderChangedMessage>();
            }, "Failed to delete reminder");
        }
        #endregion

        partial void OnShowOnlyUpcomingChanged(bool value)
        {
            LoadRemindersCommand.ExecuteAsync(null);
        }
    }
}

