using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using CommunityToolkit.Mvvm.Input;

using FocusFlow.App.Messages;
using FocusFlow.App.ViewModels;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Domain.Enums;

namespace FocusFlow.App.ViewModels.Emails
{
    public partial class EmailsViewModel : BaseViewModel
    {
        [RelayCommand]
        private async Task ShowSyncFormAsync()
        {
            await ExecuteAsync(async () =>
            {
                var accounts = await _emailAccountService.GetAllAsync();
                UpdateCollection(AvailableAccounts, accounts);

                SelectedSyncAccount = AvailableAccounts.FirstOrDefault();

                SyncMaxCount = 20;
                IsSyncFormVisible = true;

            }, "Failed to load email accounts");
        }

        [RelayCommand]
        private void CancelSyncForm()
        {
            IsSyncFormVisible = false;
            SelectedSyncAccount = null;
        }

        [RelayCommand]
        private async Task ExecuteSyncAsync()
        {
            if (SelectedSyncAccount == null)
            {
                ErrorMessage = "Please select an email account";
                return;
            }

            if (SyncMaxCount < 1 || SyncMaxCount > 500)
            {
                ErrorMessage = "Max count must be between 1 and 500";
                return;
            }

            IsSyncing = true;

            try
            {
                await ExecuteAsync(async () =>
                {
                    var account = SelectedSyncAccount;

                    EmailSyncResultDto result = account.Provider switch
                    {
                        EmailProvider.Gmail => await _emailSyncApi.SyncGmailAsync(account.Id, SyncMaxCount),
                        EmailProvider.Outlook => await _emailSyncApi.SyncOutlookAsync(account.Id, SyncMaxCount),
                        _ => throw new InvalidOperationException($"Unsupported provider: {account.Provider}")
                    };

                    IsSyncFormVisible = false;

                    var message = BuildSyncResultMessage(result);

                    MessageBox.Show(
                        message,
                        "FocusFlow",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await LoadEmailsAsync();
                    NotifyChanged<EmailChangedMessage>();

                }, "Failed to sync emails");
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private static string BuildSyncResultMessage(EmailSyncResultDto result)
        {
            var msg = $"Sync completed: {result.Added} added, {result.Skipped} skipped, {result.Failed} failed";

            if (result.Errors != null && result.Errors.Count > 0)
                msg += $"\nErrors: {string.Join(", ", result.Errors)}";

            return msg;
        }

        [RelayCommand]
        private async Task CreateTaskFromEmailAsync()
        {
            if (SelectedEmail == null) return;

            await ExecuteAsync(async () =>
            {
                string rawSubject = SelectedEmail.Subject ?? "Geen onderwerp";
                string cleanTitle = Regex.Replace(
                    rawSubject,
                    @"^(?:Fwd|Re|Doorst|Fw|Antw):\s*",
                    "",
                    RegexOptions.IgnoreCase);

                string notes = EmailSummary ?? string.Empty;
                if (string.IsNullOrWhiteSpace(notes))
                {
                    string bodyText = SelectedEmail.BodyText ?? string.Empty;
                    notes = bodyText.Length > 500
                        ? bodyText.Substring(0, 500) + "..."
                        : bodyText;
                }

                var newTask = new FocusTaskDto(
                    Guid.NewGuid(),
                    cleanTitle,
                    notes,
                    DateTime.UtcNow.AddDays(1),
                    false,
                    SelectedEmail.Id);

                await _taskService.AddAsync(newTask);

                MessageBox.Show(
                    "De taak is succesvol aangemaakt!",
                    "FocusFlow",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }, "Failed to create task from email");
        }
    }
}
