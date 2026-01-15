using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.App.Messages;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.ViewModels
{
    public partial class EmailsViewModel : BaseViewModel
    {
        private readonly IEmailService _emailService;
        private readonly ISummaryService _summaryService;

        #region Properties
        [ObservableProperty]
        private ObservableCollection<EmailItemViewModel> _emailItems = new();

        [ObservableProperty]
        private EmailDto? _selectedEmail;

        [ObservableProperty]
        private EmailItemViewModel? _selectedEmailItem;

        public bool HasSelectedEmails => EmailItems.Any(e => e.IsSelected);

        public int SelectedEmailsCount => EmailItems.Count(e => e.IsSelected);

        [ObservableProperty]
        private string? _searchQuery;

        [ObservableProperty]
        private bool _isEmailFormVisible;

        [ObservableProperty]
        private string _emailFrom = string.Empty;

        [ObservableProperty]
        private string _emailSubject = string.Empty;

        [ObservableProperty]
        private string _emailBody = string.Empty;

        [ObservableProperty]
        private DateTime _emailReceivedDate = DateTime.UtcNow;

        [ObservableProperty]
        private string? _emailSummary;

        [ObservableProperty]
        private bool _isLoadingSummary;

        [ObservableProperty]
        private int _emailPriorityScore;
        #endregion

        public EmailsViewModel(IEmailService emailService, ISummaryService summaryService)
        {
            _emailService = emailService;
            _summaryService = summaryService;
        }

        #region Commands
        [RelayCommand]
        private async Task LoadEmailsAsync()
        {
            await ExecuteAsync(async () =>
            {
                var emails = await _emailService.GetLatestAsync(SearchQuery);
                var emailItems = emails.Select(e => new EmailItemViewModel(e)).ToList();
                UpdateCollection(EmailItems, emailItems);
            }, "Failed to load emails");
        }

        [RelayCommand]
        private async Task SelectEmailAsync(EmailDto email)
        {
            SelectedEmail = email;
            EmailPriorityScore = email.PriorityScore;
            await LoadEmailSummaryAsync(email.Id);
        }

        partial void OnSelectedEmailItemChanged(EmailItemViewModel? value)
        {
            SelectedEmail = value?.Email;
            if (value != null)
            {
                EmailPriorityScore = value.Email.PriorityScore;
                LoadEmailSummaryCommand.ExecuteAsync(value.Email.Id);
            }
        }

        partial void OnEmailItemsChanged(ObservableCollection<EmailItemViewModel>? oldValue, ObservableCollection<EmailItemViewModel> newValue)
        {
            
            if (oldValue != null)
            {
                foreach (var item in oldValue)
                {
                    item.PropertyChanged -= EmailItem_PropertyChanged;
                }
            }

            
            if (newValue != null)
            {
                foreach (var item in newValue)
                {
                    item.PropertyChanged += EmailItem_PropertyChanged;
                }
            }

            OnPropertyChanged(nameof(HasSelectedEmails));
            OnPropertyChanged(nameof(SelectedEmailsCount));
        }

        private void EmailItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EmailItemViewModel.IsSelected))
            {
                OnPropertyChanged(nameof(HasSelectedEmails));
                OnPropertyChanged(nameof(SelectedEmailsCount));
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
                    0);

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
            if (SelectedEmail is null) return;

            await ExecuteAsync(async () =>
            {
                await _emailService.DeleteAsync(SelectedEmail.Id);
                SelectedEmail = null;
                await LoadEmailsAsync();
                NotifyChanged<EmailChangedMessage>();
            }, "Failed to delete email");
        }

        [RelayCommand]
        private async Task DeleteSelectedEmailsAsync()
        {
            var selectedItems = EmailItems.Where(e => e.IsSelected).ToList();
            if (selectedItems.Count == 0) return;

            await ExecuteAsync(async () =>
            {
                foreach (var item in selectedItems)
                {
                    await _emailService.DeleteAsync(item.Email.Id);
                }

                if (SelectedEmail != null && selectedItems.Any(e => e.Email.Id == SelectedEmail.Id))
                {
                    SelectedEmail = null;
                }

                await LoadEmailsAsync();
                NotifyChanged<EmailChangedMessage>();
            }, "Failed to delete selected emails");
        }
        #endregion

        partial void OnSearchQueryChanged(string? value)
        {
            LoadEmailsCommand.ExecuteAsync(null);
        }
    }
}

