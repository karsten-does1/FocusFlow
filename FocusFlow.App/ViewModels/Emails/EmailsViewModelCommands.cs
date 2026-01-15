using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private async Task LoadEmailsAsync()
        {
            await ExecuteAsync(async () =>
            {
                var emails = await _emailService.GetLatestAsync(SearchQuery);
                var emailItems = emails.Select(e => new EmailItemViewModel(e)).ToList();

                UpdateCollection(EmailItems, emailItems);

                RefreshSelection();
            }, "Failed to load emails");
        }

        [RelayCommand]
        private void RefreshSelection()
        {
            OnPropertyChanged(nameof(HasSelectedEmails));
            OnPropertyChanged(nameof(SelectedEmailsCount));
        }

        partial void OnSelectedEmailItemChanged(EmailItemViewModel? value)
        {
            SelectedEmail = value?.Email;

            if (value != null)
            {
                EmailPriorityScore = value.Email.PriorityScore;
                EmailCategory = value.Email.Category;
                EmailSuggestedAction = value.Email.SuggestedAction;

                _ = LoadEmailSummaryCommand.ExecuteAsync(value.Email.Id);
            }
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

        [RelayCommand]
        private void ShowNewEmailForm()
        {
            IsEmailFormVisible = true;
            EmailFrom = string.Empty;
            EmailSubject = string.Empty;
            EmailBody = string.Empty;
            EmailReceivedDate = DateTime.UtcNow;
        }

        [RelayCommand]
        private void CancelEmailForm()
        {
            IsEmailFormVisible = false;
        }

        [RelayCommand]
        private async Task SaveEmailAsync()
        {
            if (string.IsNullOrWhiteSpace(EmailFrom) || string.IsNullOrWhiteSpace(EmailSubject))
            {
                ErrorMessage = "From and Subject are required";
                return;
            }

            await ExecuteAsync(async () =>
            {
                var email = new EmailDto(
                    Guid.NewGuid(),
                    EmailFrom,
                    EmailSubject,
                    EmailBody,
                    EmailReceivedDate,
                    0,
                    "Overig",
                    "Lezen",
                    EmailProvider.Unknown,
                    null,
                    null);

                await _emailService.AddAsync(email);

                IsEmailFormVisible = false;

                await LoadEmailsAsync();
                NotifyChanged<EmailChangedMessage>();
            }, "Failed to save email");
        }

        [RelayCommand]
        private async Task UpdateEmailPriorityAsync()
        {
            if (SelectedEmail is null) return;

            await ExecuteAsync(async () =>
            {
                var updatedEmail = new EmailDto(
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

                await _emailService.UpdateAsync(updatedEmail);

                await LoadEmailsAsync();

                SelectedEmail = updatedEmail;
                NotifyChanged<EmailChangedMessage>();
            }, "Failed to update priority");
        }

        [RelayCommand]
        private async Task DeleteEmailAsync()
        {
            var idsToDelete = EmailItems
                .Where(e => e.IsSelected)
                .Select(e => e.Email.Id)
                .Distinct()
                .ToList();

            if (idsToDelete.Count == 0 && SelectedEmail is not null)
            {
                idsToDelete.Add(SelectedEmail.Id);
            }

            if (idsToDelete.Count == 0)
                return;

            await ExecuteAsync(async () =>
            {
                foreach (var id in idsToDelete)
                {
                    await _emailService.DeleteAsync(id);
                }

                if (SelectedEmail != null && idsToDelete.Contains(SelectedEmail.Id))
                {
                    SelectedEmail = null;
                }

                await LoadEmailsAsync();
                NotifyChanged<EmailChangedMessage>();

                RefreshSelection();
            }, "Failed to delete email(s)");
        }

        [RelayCommand]
        private async Task DeleteSelectedEmailsAsync()
        {
            await DeleteEmailAsync();
        }

        [RelayCommand]
        private async Task DeleteAllEmailsAsync()
        {
            await ExecuteAsync(async () =>
            {
                await _emailService.DeleteAllAsync();

                SelectedEmail = null;
                EmailSummary = null;

                await LoadEmailsAsync();
                NotifyChanged<EmailChangedMessage>();
                RefreshSelection();
            }, "Failed to delete all emails");
        }

        partial void OnSearchQueryChanged(string? value)
        {
            _ = LoadEmailsCommand.ExecuteAsync(null);
        }
    }
}
