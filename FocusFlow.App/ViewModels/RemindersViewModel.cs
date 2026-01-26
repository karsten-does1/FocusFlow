using System;
using System.Collections.ObjectModel;
using System.Globalization;
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

        [ObservableProperty]
        private ObservableCollection<ReminderDto> _reminders = new();

        [ObservableProperty]
        private bool _showOnlyUpcoming = true;

        [ObservableProperty]
        private bool _isReminderFormVisible;

        [ObservableProperty]
        private string _reminderTitle = string.Empty;

        [ObservableProperty]
        private DateTime _reminderFireDate = DateTime.Now.Date;

        [ObservableProperty]
        private string _reminderFireTimeText = DateTime.Now.ToString("HH:mm");

        [ObservableProperty]
        private Guid? _relatedTaskId;

        [ObservableProperty]
        private Guid? _relatedEmailId;

        public RemindersViewModel(IReminderService reminderService)
        {
            _reminderService = reminderService;
            ResetReminderFormDefaults();
        }

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

            ResetReminderFormDefaults();

            RelatedTaskId = null;
            RelatedEmailId = null;

            ErrorMessage = null;
        }

        private void ResetReminderFormDefaults()
        {
            var now = DateTime.Now;

            ReminderFireDate = now.Date;
            ReminderFireTimeText = now.ToString("HH:mm");
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

            if (!TryParseTime(ReminderFireTimeText, out var time))
            {
                ErrorMessage = "Time must be in format HH:mm (e.g. 09:30)";
                return;
            }

            await ExecuteAsync(async () =>
            {
                var local = ReminderFireDate.Date.Add(time);

                if (local.Kind == DateTimeKind.Unspecified)
                    local = DateTime.SpecifyKind(local, DateTimeKind.Local);

                var fireAtUtc = local.ToUniversalTime();

                var reminder = new ReminderDto(
                    Guid.NewGuid(),
                    ReminderTitle,
                    fireAtUtc,
                    false,
                    RelatedTaskId,
                    RelatedEmailId);

                await _reminderService.AddAsync(reminder);

                IsReminderFormVisible = false;
                await LoadRemindersAsync();

                NotifyChanged<ReminderChangedMessage>();

            }, "Failed to save reminder");
        }

        private static bool TryParseTime(string? text, out TimeSpan time)
        {
            time = default;

            var s = (text ?? string.Empty).Trim();

            return TimeSpan.TryParseExact(s, @"h\:mm", CultureInfo.InvariantCulture, out time) ||
                   TimeSpan.TryParseExact(s, @"hh\:mm", CultureInfo.InvariantCulture, out time);
        }

        [RelayCommand]
        private async Task MarkFiredAsync(ReminderDto reminder)
        {
            if (reminder == null) return;

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
            if (reminder == null) return;

            await ExecuteAsync(async () =>
            {
                await _reminderService.DeleteAsync(reminder.Id);
                await LoadRemindersAsync();
                NotifyChanged<ReminderChangedMessage>();
            }, "Failed to delete reminder");
        }

        partial void OnShowOnlyUpcomingChanged(bool value)
        {
            _ = LoadRemindersCommand.ExecuteAsync(null);
        }
    }
}
