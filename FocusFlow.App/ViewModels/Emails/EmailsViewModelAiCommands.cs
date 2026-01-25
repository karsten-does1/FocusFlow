using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.DTOs.Ai;

namespace FocusFlow.App.ViewModels.Emails
{
    public partial class EmailsViewModel
    {
        private void RefreshAiCommands()
        {
            GenerateDraftReplyCommand.NotifyCanExecuteChanged();
            GenerateComposeEmailCommand.NotifyCanExecuteChanged();

            ExtractTasksCommand.NotifyCanExecuteChanged();
            CreateSelectedAiTasksCommand.NotifyCanExecuteChanged();
            CreateSingleAiTaskCommand.NotifyCanExecuteChanged();
        }

        #region Panel Toggles & Helpers
        private void OpenOnlyDraftReply()
        {
            IsDraftReplyVisible = true;
            IsComposeVisible = false;
            IsAiTasksVisible = false;
        }

        private void OpenOnlyCompose()
        {
            IsComposeVisible = true;
            IsDraftReplyVisible = false;
            IsAiTasksVisible = false;
        }

        private void OpenOnlyAiTasks()
        {
            IsAiTasksVisible = true;
            IsDraftReplyVisible = false;
            IsComposeVisible = false;
        }

        [RelayCommand]
        private void ToggleDraftReply()
        {
            if (IsDraftReplyVisible) IsDraftReplyVisible = false;
            else OpenOnlyDraftReply();
        }

        [RelayCommand]
        private void ToggleCompose()
        {
            if (IsComposeVisible) IsComposeVisible = false;
            else OpenOnlyCompose();
        }

        [RelayCommand]
        private void ToggleAiTasks()
        {
            if (IsAiTasksVisible) IsAiTasksVisible = false;
            else OpenOnlyAiTasks();
        }
        #endregion

        #region AI: Draft Reply
        private bool CanGenerateDraftReply()
            => SelectedEmail != null && !IsAnyBusy && !IsGeneratingReply;

        [RelayCommand(CanExecute = nameof(CanGenerateDraftReply))]
        private async Task GenerateDraftReplyAsync()
        {
            if (SelectedEmail is null) return;

            try
            {
                IsGeneratingReply = true;
                DraftReply = string.Empty;
                ErrorMessage = null;

                var req = new DraftReplyRequestDto(
                    Subject: SelectedEmail.Subject ?? "",
                    Body: SelectedEmail.BodyText ?? "",
                    Sender: SelectedEmail.From,
                    ReceivedAtUtc: SelectedEmail.ReceivedUtc.ToString("O"),
                    Tone: SelectedTone,
                    Length: SelectedLength);

                var result = await _aiService.DraftReplyAsync(req);

                DraftReply = result?.Reply ?? string.Empty;

                OpenOnlyDraftReply();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to generate draft reply: {ex.Message}";
                DraftReply = string.Empty;
            }
            finally
            {
                IsGeneratingReply = false;
                RefreshAiCommands();
            }
        }
        #endregion

        #region AI: Compose Email
        private bool CanGenerateComposeEmail()
            => !IsAnyBusy
               && !IsComposingEmail
               && !string.IsNullOrWhiteSpace(ComposePrompt);

        [RelayCommand(CanExecute = nameof(CanGenerateComposeEmail))]
        private async Task GenerateComposeEmailAsync()
        {
            try
            {
                IsComposingEmail = true;
                ErrorMessage = null;

                var req = new ComposeEmailRequestDto(
                    Prompt: ComposePrompt.Trim(),
                    Subject: string.IsNullOrWhiteSpace(ComposeSubject) ? null : ComposeSubject.Trim(),
                    Instructions: string.IsNullOrWhiteSpace(ComposeInstructions) ? null : ComposeInstructions.Trim(),
                    Tone: SelectedTone,
                    Length: SelectedLength,
                    Language: null,
                    ReplyToSubject: SelectedEmail?.Subject,
                    ReplyToBody: SelectedEmail?.BodyText,
                    ReplyToSender: SelectedEmail?.From,
                    ReplyToReceivedAtUtc: SelectedEmail?.ReceivedUtc.ToString("O"));

                var result = await _aiService.ComposeEmailAsync(req);

                ComposedSubject = result?.Subject ?? string.Empty;
                ComposedBody = result?.Body ?? string.Empty;

                OpenOnlyCompose();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to compose email: {ex.Message}";
            }
            finally
            {
                IsComposingEmail = false;
                RefreshAiCommands();
            }
        }
        #endregion

        #region AI: Task Extraction
        private bool CanExtractTasks()
            => SelectedEmail != null && !IsAnyBusy && !IsExtractingTasks;

        [RelayCommand(CanExecute = nameof(CanExtractTasks))]
        private async Task ExtractTasksAsync()
        {
            if (SelectedEmail is null) return;

            IsExtractingTasks = true;
            RefreshAiCommands();

            try
            {
                await ExecuteAsync(async () =>
                {
                    ErrorMessage = null;

                    TaskSuggestions.Clear();
                    NeedsClarification.Clear();
                    OnPropertyChanged(nameof(HasTaskSuggestions));
                    OnPropertyChanged(nameof(HasClarifications));

                    var req = new ExtractTasksRequestDto(
                        Subject: SelectedEmail.Subject ?? "",
                        Body: SelectedEmail.BodyText ?? "",
                        Sender: SelectedEmail.From,
                        ReceivedAtUtc: SelectedEmail.ReceivedUtc.ToString("O"),
                        ThreadHint: null);

                    var result = await _aiService.ExtractTasksAsync(req);

                    if (result?.Tasks != null)
                    {
                        foreach (var t in result.Tasks)
                        {
                            TaskSuggestions.Add(new AiTaskSuggestionViewModel(
                                title: t.Title,
                                description: t.Description ?? "",
                                priority: string.IsNullOrWhiteSpace(t.Priority) ? "Medium" : t.Priority,
                                dueDate: t.DueDate,
                                dueText: t.DueText,
                                confidence: t.Confidence,
                                sourceQuote: t.SourceQuote));
                        }
                    }

                    if (result?.NeedsClarification is { Count: > 0 })
                    {
                        foreach (var c in result.NeedsClarification)
                            NeedsClarification.Add(c);
                    }

                    OnPropertyChanged(nameof(HasTaskSuggestions));
                    OnPropertyChanged(nameof(HasClarifications));

                    if (TaskSuggestions.Count > 0 || NeedsClarification.Count > 0)
                        OpenOnlyAiTasks();

                }, "Failed to extract tasks");
            }
            finally
            {
                IsExtractingTasks = false;
                RefreshAiCommands();
            }
        }

        private bool CanCreateSelectedAiTasks()
            => SelectedEmail != null
               && !IsAnyBusy
               && !IsExtractingTasks
               && TaskSuggestions.Any(s => s.IsSelected);

        [RelayCommand(CanExecute = nameof(CanCreateSelectedAiTasks))]
        private async Task CreateSelectedAiTasksAsync()
        {
            if (SelectedEmail is null) return;

            var selected = TaskSuggestions.Where(s => s.IsSelected).ToList();
            if (selected.Count == 0) return;

            await ExecuteAsync(async () =>
            {
                foreach (var s in selected)
                {
                    var due = ParseDueUtcOrNull(s.DueDate) ?? ParseDueUtcOrNull(s.DueText);

                    var newTask = new FocusTaskDto(
                        Guid.NewGuid(),
                        s.Title,
                        BuildNotesFromSuggestion(s),
                        due,
                        false,
                        SelectedEmail.Id);

                    await _taskService.AddAsync(newTask);
                    s.IsSelected = false;
                }

                _dialogService.ShowInfo($"{selected.Count} taak/taken aangemaakt.", "FocusFlow");

            }, "Failed to create task(s)");

            RefreshAiCommands();
        }

        private bool CanCreateSingleAiTask(AiTaskSuggestionViewModel suggestion)
            => SelectedEmail != null && !IsAnyBusy && !IsExtractingTasks && suggestion != null;

        [RelayCommand(CanExecute = nameof(CanCreateSingleAiTask))]
        private async Task CreateSingleAiTaskAsync(AiTaskSuggestionViewModel suggestion)
        {
            if (SelectedEmail is null || suggestion is null) return;

            await ExecuteAsync(async () =>
            {
                var due = ParseDueUtcOrNull(suggestion.DueDate) ?? ParseDueUtcOrNull(suggestion.DueText);

                var newTask = new FocusTaskDto(
                    Guid.NewGuid(),
                    suggestion.Title,
                    BuildNotesFromSuggestion(suggestion),
                    due,
                    false,
                    SelectedEmail.Id);

                await _taskService.AddAsync(newTask);

                suggestion.IsSelected = false;
                _dialogService.ShowInfo("Taak aangemaakt.", "FocusFlow");

            }, "Failed to create task");

            RefreshAiCommands();
        }
        #endregion

        #region Helpers (kept here for simplicity)
        private static string BuildNotesFromSuggestion(AiTaskSuggestionViewModel s)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(s.Description))
                parts.Add(s.Description.Trim());

            if (s.HasDue)
                parts.Add($"Due: {s.DueDisplay}");

            if (!string.IsNullOrWhiteSpace(s.Priority))
                parts.Add($"Priority: {s.Priority}");

            if (s.HasSource)
                parts.Add($"Quote: {s.SourceQuote}");

            return string.Join(Environment.NewLine, parts);
        }

        private static DateTime? ParseDueUtcOrNull(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            if (DateTime.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var dt))
                return dt;

            if (DateTime.TryParse(
                    raw,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.AssumeLocal,
                    out var dtLocal))
                return dtLocal.ToUniversalTime();

            return null;
        }
        #endregion

        #region Hooks
        partial void OnComposePromptChanged(string value) => RefreshAiCommands();
        #endregion
    }
}
