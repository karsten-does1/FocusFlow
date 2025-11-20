using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

           
            DashboardViewModel.LoadDashboardCommand.ExecuteAsync(null);
            TasksViewModel.LoadTasksCommand.ExecuteAsync(null);
            EmailsViewModel.LoadEmailsCommand.ExecuteAsync(null);
            RemindersViewModel.LoadRemindersCommand.ExecuteAsync(null);
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

            
            if (viewName == "Settings")
            {
                SettingsViewModel.LoadAccountsCommand.ExecuteAsync(null);
            }
        }
    }
}

