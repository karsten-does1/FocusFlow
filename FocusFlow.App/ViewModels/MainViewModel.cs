using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.App.ViewModels;
using FocusFlow.App.ViewModels.Emails;
using FocusFlow.App.ViewModels.Tasks;
using FocusFlow.App.ViewModels.Settings;

namespace FocusFlow.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableObject? _currentViewModel;

        [ObservableProperty]
        private string _selectedView = "Dashboard";

        public DashboardViewModel DashboardViewModel { get; }
        public TasksViewModel TasksViewModel { get; }
        public EmailsViewModel EmailsViewModel { get; }
        public RemindersViewModel RemindersViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        public MainViewModel(
            DashboardViewModel dashboardViewModel,
            TasksViewModel tasksViewModel,
            EmailsViewModel emailsViewModel,
            RemindersViewModel remindersViewModel,
            SettingsViewModel settingsViewModel)
        {
            DashboardViewModel = dashboardViewModel;
            TasksViewModel = tasksViewModel;
            EmailsViewModel = emailsViewModel;
            RemindersViewModel = remindersViewModel;
            SettingsViewModel = settingsViewModel;

            CurrentViewModel = DashboardViewModel;
            SelectedView = "Dashboard";

            
            _ = DashboardViewModel.LoadDashboardCommand.ExecuteAsync(null);
            _ = TasksViewModel.LoadTasksCommand.ExecuteAsync(null);
            _ = EmailsViewModel.LoadEmailsCommand.ExecuteAsync(null);
            _ = RemindersViewModel.LoadRemindersCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private void NavigateTo(string viewName)
        {
            SelectedView = viewName;
            CurrentViewModel = viewName switch
            {
                "Dashboard" => DashboardViewModel,
                "Tasks" => TasksViewModel,
                "Emails" => EmailsViewModel,
                "Reminders" => RemindersViewModel,
                "Settings" => SettingsViewModel,
                _ => DashboardViewModel
            };

            
            if (viewName == "Dashboard")
                _ = DashboardViewModel.LoadDashboardCommand.ExecuteAsync(null);

            if (viewName == "Tasks")
                _ = TasksViewModel.LoadTasksCommand.ExecuteAsync(null);

            if (viewName == "Emails")
                _ = EmailsViewModel.LoadEmailsCommand.ExecuteAsync(null);

            if (viewName == "Reminders")
                _ = RemindersViewModel.LoadRemindersCommand.ExecuteAsync(null);

            if (viewName == "Settings")
                _ = SettingsViewModel.LoadAccountsCommand.ExecuteAsync(null);
        }
    }
}