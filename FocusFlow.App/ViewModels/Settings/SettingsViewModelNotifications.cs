using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FocusFlow.App.Messages;
using FocusFlow.Core.Application.Contracts.DTOs.Settings;

namespace FocusFlow.App.ViewModels.Settings
{
    public partial class SettingsViewModel
    {
        [ObservableProperty] private bool _notificationsEnabled = true;
        [ObservableProperty] private int _notificationTickSeconds = 60;
        [ObservableProperty] private int _reminderUpcomingWindowMinutes = 5;

        [ObservableProperty] private bool _briefingNotificationsEnabled = true;
        [ObservableProperty] private string _briefingTimeLocal = "09:00";

        [ObservableProperty] private bool _isLoadingNotificationSettings;
        [ObservableProperty] private bool _isSavingNotificationSettings;

        public int[] TickOptionsSeconds { get; } = { 10, 30, 60, 120, 300 };
        public int[] UpcomingWindowOptionsMinutes { get; } = { 1, 5, 10, 15, 30, 60 };

        [RelayCommand]
        private async Task LoadNotificationSettingsAsync()
        {
            IsLoadingNotificationSettings = true;

            try
            {
                await ExecuteAsync(async () =>
                {
                    var dto = await _settingsApi.GetAsync();

                    NotificationsEnabled = dto.Notifications.Enabled;
                    NotificationTickSeconds = dto.Notifications.TickSeconds;
                    ReminderUpcomingWindowMinutes = dto.Notifications.ReminderUpcomingWindowMinutes;

                    BriefingNotificationsEnabled = dto.Notifications.BriefingEnabled;
                    BriefingTimeLocal = dto.Notifications.BriefingTimeLocal;
                }, "Failed to load notification settings");
            }
            finally
            {
                IsLoadingNotificationSettings = false;
            }
        }

        [RelayCommand]
        private async Task SaveNotificationSettingsAsync()
        {
            IsSavingNotificationSettings = true;

            try
            {
                var current = await _settingsApi.GetAsync();

                var dto = new AppSettingsDto(
                    Briefing: current.Briefing,
                    Notifications: new NotificationSettingsDto(
                        Enabled: NotificationsEnabled,
                        TickSeconds: NotificationTickSeconds,
                        ReminderUpcomingWindowMinutes: ReminderUpcomingWindowMinutes,
                        BriefingEnabled: BriefingNotificationsEnabled,
                        BriefingTimeLocal: BriefingTimeLocal
                    )
                );

                var saved = await _settingsApi.UpdateAsync(dto);

                NotificationsEnabled = saved.Notifications.Enabled;
                NotificationTickSeconds = saved.Notifications.TickSeconds;
                ReminderUpcomingWindowMinutes = saved.Notifications.ReminderUpcomingWindowMinutes;

                BriefingNotificationsEnabled = saved.Notifications.BriefingEnabled;
                BriefingTimeLocal = saved.Notifications.BriefingTimeLocal;

                WeakReferenceMessenger.Default.Send(NotificationSettingsSavedMessage.Instance);

                MessageBox.Show(
                    "Notification settings applied.",
                    "Settings saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save notification settings: {ex.Message}";

                MessageBox.Show(
                    "Opslaan mislukt. Probeer opnieuw.\n\n" + ex.Message,
                    "Settings failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsSavingNotificationSettings = false;
            }
        }
    }
}
