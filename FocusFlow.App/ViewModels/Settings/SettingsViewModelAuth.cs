using System;
using System.Diagnostics;

using CommunityToolkit.Mvvm.Input;

namespace FocusFlow.App.ViewModels.Settings
{
    public partial class SettingsViewModel : BaseViewModel
    {
        [RelayCommand]
        private void ConnectGmail()
        {
            OpenAuthUrl("/api/auth/google/login");
        }

        [RelayCommand]
        private void ConnectOutlook()
        {
            OpenAuthUrl("/api/auth/microsoft/login");
        }

        private void OpenAuthUrl(string path)
        {
            try
            {
                var apiBaseUrl = _configuration["Api:BaseUrl"] ?? "https://localhost:7248";
                var authUrl = $"{apiBaseUrl}{path}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to open login: {ex.Message}";
            }
        }
    }
}
