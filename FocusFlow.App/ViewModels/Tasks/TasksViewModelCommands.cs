using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.App.Messages;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.App.ViewModels.Tasks
{
    public partial class TasksViewModel : BaseViewModel
    {
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
            SelectedTask = null;
            TaskTitle = string.Empty;
            TaskNotes = null;
            TaskDueDate = null; 
            TaskIsDone = false;

            IsTaskFormVisible = true;
        }

        [RelayCommand]
        private void ShowEditTaskForm(FocusTaskDto task)
        {
            if (task == null) return;

            IsEditing = true;
            SelectedTask = task;
            TaskTitle = task.Title;
            TaskNotes = task.Notes;

            TaskDueDate = task.DueUtc?.ToLocalTime();

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
                DateTime? dueUtc = null;
                if (TaskDueDate.HasValue)
                {
                    var local = TaskDueDate.Value;
                    if (local.Kind == DateTimeKind.Unspecified)
                        local = DateTime.SpecifyKind(local, DateTimeKind.Local);

                    dueUtc = local.ToUniversalTime();
                }

                if (IsEditing && SelectedTask != null)
                {
                    var updatedTask = new FocusTaskDto(
                        SelectedTask.Id,
                        TaskTitle,
                        TaskNotes,
                        dueUtc,
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
                        dueUtc,
                        TaskIsDone,
                        null);

                    await _taskService.AddAsync(newTask);
                }

                IsTaskFormVisible = false;
                await LoadTasksAsync();
                NotifyChanged<TaskChangedMessage>();

            }, "Failed to save task");
        }

        [RelayCommand]
        private async Task ToggleTaskDoneAsync(FocusTaskDto task)
        {
            if (task == null) return;

            await ExecuteAsync(async () =>
            {
                var updatedTask = new FocusTaskDto(
                    task.Id,
                    task.Title,
                    task.Notes,
                    task.DueUtc, 
                    !task.IsDone,
                    task.RelatedEmailId);

                await _taskService.UpdateAsync(updatedTask);

                await LoadTasksAsync();
                NotifyChanged<TaskChangedMessage>();

            }, "Failed to update task");
        }

        [RelayCommand]
        private async Task DeleteTaskAsync(FocusTaskDto task)
        {
            if (task == null) return;

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
            FilterDone = filter switch
            {
                "Active" => false,
                "Completed" => true,
                _ => null
            };

            await LoadTasksAsync();
        }

        partial void OnFilterDoneChanged(bool? value)
        {
            _ = LoadTasksCommand.ExecuteAsync(null);
        }
    }
}
