using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using FocusFlow.App.Messages;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Domain.Enums;

namespace FocusFlow.App.ViewModels.Emails
{
    public partial class EmailsViewModel
    {
        #region Sync Form & Execution

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
                    _dialogService.ShowInfo(message, "FocusFlow");

                    await LoadEmailsAsync();
                    NotifyChanged<EmailChangedMessage>();

                    RefreshMailCommands();

                }, "Failed to sync emails");
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private static string BuildSyncResultMessage(EmailSyncResultDto result)
        {
            var lines = new List<string>
            {
                "Sync completed:",
                $"- {result.Added} e-mails toegevoegd",
                $"- {result.Skipped} overgeslagen",
                $"- {result.Failed} mislukt"
            };

            if (result.Errors is { Count: > 0 })
            {
                lines.Add(string.Empty);
                lines.Add("Fouten:");
                lines.AddRange(result.Errors.Select(e => $"- {e}"));
            }

            return string.Join(Environment.NewLine, lines);
        }

        #endregion

        #region Manual Task Creation

        private bool CanCreateTaskFromEmail() => SelectedEmail != null && !IsAnyBusy;

        [RelayCommand(CanExecute = nameof(CanCreateTaskFromEmail))]
        private async Task CreateTaskFromEmailAsync()
        {
            if (SelectedEmail == null) return;

            await ExecuteAsync(async () =>
            {
                string title = BuildTaskTitle(SelectedEmail.Subject);
                string notes = BuildTaskNotes(EmailSummary, SelectedEmail.BodyText);

                var newTask = new FocusTaskDto(
                    Guid.NewGuid(),
                    title,
                    notes,
                    DateTime.UtcNow.AddDays(1),
                    false,
                    SelectedEmail.Id);

                await _taskService.AddAsync(newTask);

                _dialogService.ShowInfo("De taak is succesvol aangemaakt!", "FocusFlow");

            }, "Failed to create task from email");
        }

        private static string BuildTaskTitle(string? subject)
        {
            var rawTitle = subject ?? "Geen onderwerp";

            return Regex.Replace(
                rawTitle,
                @"^(?:Fwd|Re|Doorst|Fw|Antw):\s*",
                string.Empty,
                RegexOptions.IgnoreCase);
        }

        private static string BuildTaskNotes(string? summary, string? bodyText)
        {
            if (!string.IsNullOrWhiteSpace(summary))
                return summary;

            var body = bodyText ?? string.Empty;

            return body.Length > 500
                ? body[..500] + "..."
                : body;
        }

        #endregion
    }
}
