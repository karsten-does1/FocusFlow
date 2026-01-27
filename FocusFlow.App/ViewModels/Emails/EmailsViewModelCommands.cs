using System;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using FocusFlow.App.Messages;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Domain.Enums;

namespace FocusFlow.App.ViewModels.Emails
{
    public partial class EmailsViewModel
    {
        private void RefreshMailCommands()
        {
            UpdateEmailPriorityCommand.NotifyCanExecuteChanged();
            DeleteEmailCommand.NotifyCanExecuteChanged();
            DeleteSelectedEmailsCommand.NotifyCanExecuteChanged();
            DeleteAllEmailsCommand.NotifyCanExecuteChanged();

            CreateTaskFromEmailCommand.NotifyCanExecuteChanged();

            RefreshAiCommands();
        }

        #region Email Load & Selection

        [RelayCommand]
        private async Task LoadEmailsAsync()
        {
            await ExecuteAsync(async () =>
            {
                var emails = await _emailService.GetLatestAsync(SearchQuery);

                emails = SelectedSortOption switch
                {
                    EmailSortOption.Oldest => emails.OrderBy(e => e.ReceivedUtc).ToList(),
                    EmailSortOption.HighestPriority => emails
                        .OrderByDescending(e => e.PriorityScore)
                        .ThenByDescending(e => e.ReceivedUtc)
                        .ToList(),
                    _ => emails.OrderByDescending(e => e.ReceivedUtc).ToList()
                };

                var items = emails.Select(e => new EmailItemViewModel(e)).ToList();

                UpdateCollection(EmailItems, items);

                SelectedEmailItem = null;
                SelectedEmail = null;

                ResetAiUiState(closePanels: true);

                RefreshSelection();

            }, "Failed to load emails");

            RefreshMailCommands();
        }

        [RelayCommand]
        private void RefreshSelection()
        {
            OnPropertyChanged(nameof(HasSelectedEmails));
            OnPropertyChanged(nameof(SelectedEmailsCount));

            RefreshMailCommands();
        }

        partial void OnSelectedEmailItemChanged(EmailItemViewModel? value)
        {
            SelectedEmail = value?.Email;

            ResetAiUiState(closePanels: true);

            if (value != null)
            {
                EmailPriorityScore = value.Email.PriorityScore;
                EmailCategory = value.Email.Category;
                EmailSuggestedAction = value.Email.SuggestedAction;

                _ = LoadEmailSummaryCommand.ExecuteAsync(value.Email.Id);
            }

            RefreshMailCommands();
        }

        [RelayCommand]
        private async Task LoadEmailSummaryAsync(Guid emailId)
        {
            try
            {
                IsLoadingSummary = true;
                var summary = await _summaryService.GetByEmailIdAsync(emailId);
                EmailSummary = summary?.Text;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load summary: {ex.Message}";
            }
            finally
            {
                IsLoadingSummary = false;
            }
        }

        #endregion

        #region Email Update & Delete

        private bool CanUpdatePriority() => SelectedEmail != null && !IsAnyBusy;

        [RelayCommand(CanExecute = nameof(CanUpdatePriority))]
        private async Task UpdateEmailPriorityAsync()
        {
            if (SelectedEmail is null) return;

            await ExecuteAsync(async () =>
            {
                var updated = new EmailDto(
                    SelectedEmail.Id,
                    SelectedEmail.From,
                    SelectedEmail.Subject,
                    SelectedEmail.BodyText,
                    SelectedEmail.ReceivedUtc,
                    EmailPriorityScore,
                    SelectedEmail.Category,
                    SelectedEmail.SuggestedAction,
                    SelectedEmail.Provider,
                    SelectedEmail.ExternalMessageId,
                    SelectedEmail.EmailAccountId);

                await _emailService.UpdateAsync(updated);

                await LoadEmailsAsync();
                SelectedEmail = updated;

                NotifyChanged<EmailChangedMessage>();

            }, "Failed to update priority");

            RefreshMailCommands();
        }

        private bool CanDeleteEmail() => (SelectedEmail != null || HasSelectedEmails) && !IsAnyBusy;

        [RelayCommand(CanExecute = nameof(CanDeleteEmail))]
        private async Task DeleteEmailAsync()
        {
            var ids = EmailItems
                .Where(e => e.IsSelected)
                .Select(e => e.Email.Id)
                .Distinct()
                .ToList();

            if (ids.Count == 0 && SelectedEmail is not null)
                ids.Add(SelectedEmail.Id);

            if (ids.Count == 0) return;

            await ExecuteAsync(async () =>
            {
                foreach (var id in ids)
                    await _emailService.DeleteAsync(id);

                if (SelectedEmail != null && ids.Contains(SelectedEmail.Id))
                {
                    SelectedEmail = null;
                    SelectedEmailItem = null;
                    ResetAiUiState(closePanels: true);
                }

                await LoadEmailsAsync();
                NotifyChanged<EmailChangedMessage>();

            }, "Failed to delete email(s)");

            RefreshMailCommands();
        }

        private bool CanDeleteSelectedEmails() => HasSelectedEmails && !IsAnyBusy;

        [RelayCommand(CanExecute = nameof(CanDeleteSelectedEmails))]
        private Task DeleteSelectedEmailsAsync() => DeleteEmailAsync();

        private bool CanDeleteAllEmails() => EmailItems.Count > 0 && !IsAnyBusy;

        [RelayCommand(CanExecute = nameof(CanDeleteAllEmails))]
        private async Task DeleteAllEmailsAsync()
        {
            await ExecuteAsync(async () =>
            {
                await _emailService.DeleteAllAsync();

                SelectedEmail = null;
                SelectedEmailItem = null;
                ResetAiUiState(closePanels: true);

                await LoadEmailsAsync();
                NotifyChanged<EmailChangedMessage>();

            }, "Failed to delete all emails");

            RefreshMailCommands();
        }

        #endregion

        #region Hooks
        partial void OnSelectedEmailChanged(EmailDto? value) => RefreshMailCommands();
        partial void OnSearchQueryChanged(string? value) => StartSearchDebounce();
        #endregion
    }
}
