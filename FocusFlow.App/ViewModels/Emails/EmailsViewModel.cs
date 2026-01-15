using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using FocusFlow.App.Services;
using FocusFlow.App.ViewModels;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.ViewModels.Emails
{
    public partial class EmailsViewModel : BaseViewModel
    {
        private readonly IEmailService _emailService;
        private readonly ISummaryService _summaryService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly EmailSyncApi _emailSyncApi;
        private readonly ITaskService _taskService;

        public EmailsViewModel(
            IEmailService emailService,
            ISummaryService summaryService,
            IEmailAccountService emailAccountService,
            EmailSyncApi emailSyncApi,
            ITaskService taskService)
        {
            _emailService = emailService;
            _summaryService = summaryService;
            _emailAccountService = emailAccountService;
            _emailSyncApi = emailSyncApi;
            _taskService = taskService;

            EmailItems.CollectionChanged += OnEmailItemsCollectionChanged;
        }

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

        [ObservableProperty]
        private string _emailCategory = "Overig";

        [ObservableProperty]
        private string _emailSuggestedAction = "Lezen";

        [ObservableProperty]
        private bool _isSyncFormVisible;

        [ObservableProperty]
        private ObservableCollection<EmailAccountDto> _availableAccounts = new();

        [ObservableProperty]
        private EmailAccountDto? _selectedSyncAccount;

        [ObservableProperty]
        private int _syncMaxCount = 20;

        [ObservableProperty]
        private bool _isSyncing;

        private void OnEmailItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (EmailItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += OnEmailItemPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (EmailItemViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= OnEmailItemPropertyChanged;
                }
            }

            RefreshSelection();
        }

        private void OnEmailItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EmailItemViewModel.IsSelected))
            {
                RefreshSelection();
            }
        }
    }
}
