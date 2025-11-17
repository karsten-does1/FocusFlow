using System.Windows;
using FocusFlow.App.ViewModels;

namespace FocusFlow.App
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
