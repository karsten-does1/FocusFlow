using System;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.App.ViewModels.Tasks
{
    public partial class TasksViewModel : BaseViewModel
    {
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
    }
}
