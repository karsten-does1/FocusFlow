using CommunityToolkit.Mvvm.ComponentModel;
using FocusFlow.Core.Application.Contracts.DTOs;

namespace FocusFlow.App.ViewModels
{
    public partial class EmailItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public EmailDto Email { get; }

        public EmailItemViewModel(EmailDto email)
        {
            Email = email;
        }
    }
}

