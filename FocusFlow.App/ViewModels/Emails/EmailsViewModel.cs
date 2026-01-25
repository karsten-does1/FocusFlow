using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using FocusFlow.App.Services;
using FocusFlow.App.ViewModels;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.ViewModels.Emails
{
    public partial class EmailsViewModel : BaseViewModel
    {
        #region Services

        private readonly IEmailService _emailService;
        private readonly ISummaryService _summaryService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly EmailSyncApi _emailSyncApi;
        private readonly ITaskService _taskService;
        private readonly IAiService _aiService;
        private readonly IDialogService _dialogService;

        #endregion

        #region Timers

        private readonly DispatcherTimer _searchTimer;

        #endregion

        #region Constructor

        public EmailsViewModel(
            IEmailService emailService,
            ISummaryService summaryService,
            IEmailAccountService emailAccountService,
            EmailSyncApi emailSyncApi,
            ITaskService taskService,
            IAiService aiService,
            IDialogService dialogService)
        {
            _emailService = emailService;
            _summaryService = summaryService;
            _emailAccountService = emailAccountService;
            _emailSyncApi = emailSyncApi;
            _taskService = taskService;
            _aiService = aiService;
            _dialogService = dialogService;

            EmailItems.CollectionChanged += OnEmailItemsCollectionChanged;

            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _searchTimer.Tick += async (_, __) =>
            {
                _searchTimer.Stop();
                await LoadEmailsAsync();
            };
        }

        #endregion

        #region Busy state

        public bool IsAnyBusy =>
            IsLoadingSummary
            || IsSyncing
            || IsGeneratingReply
            || IsComposingEmail
            || IsExtractingTasks;

        partial void OnIsLoadingSummaryChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAnyBusy));
            RefreshMailCommands();
        }

        partial void OnIsSyncingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAnyBusy));
            RefreshMailCommands();
        }

        partial void OnIsGeneratingReplyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAnyBusy));
            RefreshMailCommands();
        }

        partial void OnIsComposingEmailChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAnyBusy));
            RefreshMailCommands();
        }

        partial void OnIsExtractingTasksChanged(bool value)
        {
            OnPropertyChanged(nameof(IsAnyBusy));
            RefreshMailCommands();
        }

        #endregion

        #region Collections & Selection

        [ObservableProperty]
        private ObservableCollection<EmailItemViewModel> _emailItems = new();

        [ObservableProperty]
        private EmailDto? _selectedEmail;

        [ObservableProperty]
        private EmailItemViewModel? _selectedEmailItem;

        public bool HasSelectedEmails => EmailItems.Any(e => e.IsSelected);
        public int SelectedEmailsCount => EmailItems.Count(e => e.IsSelected);

        #endregion

        #region Search

        [ObservableProperty]
        private string? _searchQuery;

        #endregion

        #region Email details 

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

        #endregion

        #region Summary & Classification

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

        #endregion

        #region Sync

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

        #endregion

        #region Panel visibility

        [ObservableProperty]
        private bool _isDraftReplyVisible;

        [ObservableProperty]
        private bool _isComposeVisible;

        [ObservableProperty]
        private bool _isAiTasksVisible;

        #endregion

        #region Draft reply

        [ObservableProperty]
        private string _draftReply = string.Empty;

        [ObservableProperty]
        private string _selectedTone = "Neutral";

        [ObservableProperty]
        private string _selectedLength = "Medium";

        [ObservableProperty]
        private bool _isGeneratingReply;

        #endregion

        #region Compose email

        [ObservableProperty]
        private string _composePrompt = string.Empty;

        [ObservableProperty]
        private string _composeSubject = string.Empty;

        [ObservableProperty]
        private string _composeInstructions = string.Empty;

        [ObservableProperty]
        private bool _isComposingEmail;

        [ObservableProperty]
        private string _composedSubject = string.Empty;

        [ObservableProperty]
        private string _composedBody = string.Empty;

        #endregion

        #region AI task extraction

        [ObservableProperty]
        private ObservableCollection<AiTaskSuggestionViewModel> _taskSuggestions = new();

        [ObservableProperty]
        private ObservableCollection<string> _needsClarification = new();

        [ObservableProperty]
        private bool _isExtractingTasks;

        public bool HasTaskSuggestions => TaskSuggestions.Count > 0;
        public bool HasClarifications => NeedsClarification.Count > 0;

        #endregion

        #region Helpers

        private void ClearError() => ErrorMessage = null;

        private void ClosePanels()
        {
            IsDraftReplyVisible = false;
            IsComposeVisible = false;
            IsAiTasksVisible = false;
        }

        private void ResetSummary()
        {
            EmailSummary = null;
            IsLoadingSummary = false;
        }

        private void ResetDraft()
        {
            DraftReply = string.Empty;
            IsGeneratingReply = false;

            SelectedTone = "Neutral";
            SelectedLength = "Medium";
        }

        private void ResetCompose()
        {
            ComposePrompt = string.Empty;
            ComposeSubject = string.Empty;
            ComposeInstructions = string.Empty;

            ComposedSubject = string.Empty;
            ComposedBody = string.Empty;

            IsComposingEmail = false;
        }

        private void ResetAiTasks()
        {
            TaskSuggestions.Clear();
            NeedsClarification.Clear();
            IsExtractingTasks = false;

            OnPropertyChanged(nameof(HasTaskSuggestions));
            OnPropertyChanged(nameof(HasClarifications));
        }

        private void ResetAiUiState(bool closePanels = true)
        {
            if (closePanels) ClosePanels();
            ResetSummary();
            ResetDraft();
            ResetCompose();
            ResetAiTasks();
            ClearError();
        }

        private void StartSearchDebounce()
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        #endregion

        #region Collection handlers

        private void OnEmailItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (EmailItemViewModel item in e.NewItems)
                    item.PropertyChanged += OnEmailItemPropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (EmailItemViewModel item in e.OldItems)
                    item.PropertyChanged -= OnEmailItemPropertyChanged;
            }

            RefreshSelection();
        }

        private void OnEmailItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EmailItemViewModel.IsSelected))
                RefreshSelection();
        }

        #endregion
    }
}
