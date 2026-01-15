using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.App.Messages;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.ViewModels
{
    public partial class TasksViewModel : BaseViewModel
    {
        private readonly ITaskService _taskService;

        #region Properties
        [ObservableProperty]
        private ObservableCollection<FocusTaskDto> _tasks = new();

        [ObservableProperty]
        private bool? _filterDone = null;

        [ObservableProperty]
        private FocusTaskDto? _selectedTask;

        [ObservableProperty]
        private string _taskTitle = string.Empty;

        [ObservableProperty]
        private string? _taskNotes;

        [ObservableProperty]
        private DateTime? _taskDueDate;

        [ObservableProperty]
        private bool _isTaskFormVisible;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private bool _taskIsDone;
        #endregion

        public TasksViewModel(ITaskService taskService)
        {
            _taskService = taskService;
        }

        #region Commands
        [RelayCommand]
        private async Task LoadTasksAsync()
        {
            await ExecuteAsync(async () =>
            {
                var tasks = await _taskService.ListAsync(FilterDone);
                UpdateCollection(Tasks, tasks);
            }, "Failed to load tasks");
        }

        [RelayCommand]
        private void ShowNewTaskForm()
        {
            IsEditing = false;
            TaskTitle = string.Empty;
            TaskNotes = null;
            TaskDueDate = null;
            TaskIsDone = false;
            SelectedTask = null;
            IsTaskFormVisible = true;
        }

        [RelayCommand]
        private void ShowEditTaskForm(FocusTaskDto task)
        {
            IsEditing = true;
            SelectedTask = task;
            TaskTitle = task.Title;
            TaskNotes = task.Notes;
            TaskDueDate = task.DueUtc;
            TaskIsDone = task.IsDone;
            IsTaskFormVisible = true;
        }

        [RelayCommand]
        private void CancelTaskForm()
        {
            IsTaskFormVisible = false;
            SelectedTask = null;
        }

        [RelayCommand]
        private async Task SaveTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(TaskTitle))
            {
                ErrorMessage = "Task title is required";
                return;
            }

            await ExecuteAsync(async () =>
            {
                if (IsEditing && SelectedTask != null)
                {
                    var updatedTask = new FocusTaskDto(
                        SelectedTask.Id,
                        TaskTitle,
                        TaskNotes,
                        TaskDueDate,
                        TaskIsDone,
                        SelectedTask.RelatedEmailId);
                    await _taskService.UpdateAsync(updatedTask);
                }
                else
                {
                    var newTask = new FocusTaskDto(
                        Guid.NewGuid(),
                        TaskTitle,
                        TaskNotes,
                        TaskDueDate,
                        TaskIsDone,
                        null);
                    await _taskService.AddAsync(newTask);
                }

                IsTaskFormVisible = false;
                await AdjustFilterAndReloadAsync(TaskIsDone);
                NotifyChanged<TaskChangedMessage>();
            }, "Failed to save task");
        }

        [RelayCommand]
        private async Task ToggleTaskDoneAsync(FocusTaskDto task)
        {
            if (task is null) return;

            await ExecuteAsync(async () =>
            {
                var newIsDone = !task.IsDone;
                var updatedTask = new FocusTaskDto(
                    task.Id,
                    task.Title,
                    task.Notes,
                    task.DueUtc,
                    newIsDone,
                    task.RelatedEmailId);

                await _taskService.UpdateAsync(updatedTask);

                var index = Tasks.IndexOf(task);
                if (index >= 0)
                {
                    Tasks[index] = updatedTask;
                }

                await AdjustFilterAndReloadAsync(newIsDone);
                NotifyChanged<TaskChangedMessage>();
            }, "Failed to update task");
        }

        [RelayCommand]
        private async Task DeleteTaskAsync(FocusTaskDto task)
        {
            await ExecuteAsync(async () =>
            {
                await _taskService.DeleteAsync(task.Id);
                await LoadTasksAsync();
                NotifyChanged<TaskChangedMessage>();
            }, "Failed to delete task");
        }

        [RelayCommand]
        private async Task FilterTasksAsync(string? filter)
        {
            bool? done = filter switch
            {
                "Active" => false,
                "Completed" => true,
                _ => null 
            };

            FilterDone = done;
            await LoadTasksAsync();
        }

        private async Task AdjustFilterAndReloadAsync(bool isDone)
        {
            if (isDone && FilterDone == false)
            {
                FilterDone = true;
                await LoadTasksAsync();
            }
            else if (!isDone && FilterDone == true)
            {
                FilterDone = false;
                await LoadTasksAsync();
            }
            else
            {
                await LoadTasksAsync();
            }
        }
        #endregion

        partial void OnFilterDoneChanged(bool? value)
        {
            LoadTasksCommand.ExecuteAsync(null);
        }
    }
}