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
        [ObservableProperty] private int _briefingTasksHours = 48;
        [ObservableProperty] private int _briefingRemindersHours = 24;
        [ObservableProperty] private int _briefingEmailsDays = 2;

        [ObservableProperty] private string _briefingSpeechMode = "Expanded";

        [ObservableProperty] private bool _isSavingBriefingSettings;
        [ObservableProperty] private bool _isLoadingBriefingSettings;

        [RelayCommand]
        private async Task LoadBriefingSettingsAsync()
        {
            IsLoadingBriefingSettings = true;

            try
            {
                await ExecuteAsync(async () =>
                {
                    var dto = await _settingsApi.GetAsync();

                    BriefingTasksHours = dto.Briefing.TasksHours;
                    BriefingRemindersHours = dto.Briefing.RemindersHours;
                    BriefingEmailsDays = dto.Briefing.EmailsDays;

                    BriefingSpeechMode = string.IsNullOrWhiteSpace(dto.Briefing.SpeechMode)
                        ? "Expanded"
                        : dto.Briefing.SpeechMode;
                }, "Failed to load briefing settings");
            }
            finally
            {
                IsLoadingBriefingSettings = false;
            }
        }

        [RelayCommand]
        private async Task SaveBriefingSettingsAsync()
        {
            IsSavingBriefingSettings = true;

            try
            {
                await ExecuteAsync(async () =>
                {
                    var current = await _settingsApi.GetAsync();

                    var dto = new AppSettingsDto(
                        Briefing: new BriefingSettingsDto(
                            TasksHours: BriefingTasksHours,
                            RemindersHours: BriefingRemindersHours,
                            EmailsDays: BriefingEmailsDays,
                            SpeechMode: BriefingSpeechMode
                        ),
                        Notifications: current.Notifications
                    );

                    var saved = await _settingsApi.UpdateAsync(dto);

                    BriefingTasksHours = saved.Briefing.TasksHours;
                    BriefingRemindersHours = saved.Briefing.RemindersHours;
                    BriefingEmailsDays = saved.Briefing.EmailsDays;

                    BriefingSpeechMode = saved.Briefing.SpeechMode;

                    WeakReferenceMessenger.Default.Send(BriefingSettingsSavedMessage.Instance);

                    MessageBox.Show(
                        "Daily briefing settings applied.",
                        "Settings saved",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }, "Failed to save briefing settings");
            }
            finally
            {
                IsSavingBriefingSettings = false;
            }
        }
    }
}
