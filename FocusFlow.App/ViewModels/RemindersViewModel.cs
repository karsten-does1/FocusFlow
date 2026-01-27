using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
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

        private enum ReminderFilter
        {
            All,
            Upcoming,
            NextDays,
            Done
        }

        [ObservableProperty]
        private ObservableCollection<ReminderDto> _remindersView = new();

        [ObservableProperty]
        private int _shownCount;

        [ObservableProperty]
        private ObservableCollection<int> _nextDaysOptions = new(new[] { 1, 3, 7, 14, 30 });

        [ObservableProperty]
        private int _selectedNextDays = 1;

        private ReminderFilter _filter = ReminderFilter.Upcoming;

        private bool _sortByDate;

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

        private IReadOnlyList<ReminderDto> _allReminders = Array.Empty<ReminderDto>();

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
                _allReminders = await _reminderService.GetAllAsync(CancellationToken.None);
                ApplyFilter();
            }, "Failed to load reminders");
        }

        [RelayCommand]
        private void SetFilterAll()
        {
            ToggleSortIfSameFilter(ReminderFilter.All);
            ApplyFilter();
        }

        [RelayCommand]
        private void SetFilterUpcoming()
        {
            ToggleSortIfSameFilter(ReminderFilter.Upcoming);
            ApplyFilter();
        }

        [RelayCommand]
        private void SetFilterNextDays()
        {
            ToggleSortIfSameFilter(ReminderFilter.NextDays);
            ApplyFilter();
        }

        [RelayCommand]
        private void SetFilterDone()
        {
            ToggleSortIfSameFilter(ReminderFilter.Done);
            ApplyFilter();
        }

        private void ToggleSortIfSameFilter(ReminderFilter requested)
        {
            if (_filter == requested)
            {
                _sortByDate = !_sortByDate;
                return;
            }

            _filter = requested;
            _sortByDate = false;
        }

        partial void OnSelectedNextDaysChanged(int value)
        {
            if (_filter == ReminderFilter.NextDays)
            {
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            var nowUtc = DateTime.UtcNow;
            var maxUtc = nowUtc.AddDays(Math.Max(1, SelectedNextDays));

            var query = _allReminders.AsEnumerable();

            switch (_filter)
            {
                case ReminderFilter.All:
                    break;

                case ReminderFilter.Upcoming:
                    query = query.Where(r => !r.Fired && r.FireAtUtc >= nowUtc);
                    break;

                case ReminderFilter.NextDays:
                    query = query.Where(r => !r.Fired && r.FireAtUtc >= nowUtc && r.FireAtUtc <= maxUtc);
                    break;

                case ReminderFilter.Done:
                    query = query.Where(r => r.Fired);
                    break;
            }

            if (_sortByDate)
            {
                query = query.OrderBy(r => r.FireAtUtc);
            }

            var list = query.ToList();
            UpdateCollection(RemindersView, list);
            ShownCount = list.Count;
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
        private async Task MarkDoneAsync(ReminderDto reminder)
        {
            if (reminder == null) return;

            await ExecuteAsync(async () =>
            {
                await _reminderService.MarkFiredAsync(reminder.Id);
                await LoadRemindersAsync();
                NotifyChanged<ReminderChangedMessage>();
            }, "Failed to mark reminder as done");
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
    }
}
