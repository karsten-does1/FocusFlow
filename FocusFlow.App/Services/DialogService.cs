using FocusFlow.Core.Application.Contracts.Services;
using System.Windows;

namespace FocusFlow.App.Services
{
    public sealed class DialogService : IDialogService
    {
        public void ShowInfo(string message, string title = "FocusFlow")
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public void ShowError(string message, string title = "FocusFlow")
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
