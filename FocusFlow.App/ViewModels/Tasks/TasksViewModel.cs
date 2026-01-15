using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.ViewModels.Tasks
{
    public partial class TasksViewModel : BaseViewModel
    {
        private readonly ITaskService _taskService;

        public TasksViewModel(ITaskService taskService)
        {
            _taskService = taskService;
        }
    }
}
