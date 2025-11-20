using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Enums;

namespace FocusFlow.App.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly IEmailAccountService _emailAccountService;

        #region Properties
        [ObservableProperty]
        private ObservableCollection<EmailAccountDto> _emailAccounts = new();

        [ObservableProperty]
        private EmailAccountDto? _selectedAccount;

        [ObservableProperty]
        private bool _isAccountFormVisible;

        [ObservableProperty]
        private string _accountDisplayName = string.Empty;

        [ObservableProperty]
        private string _accountEmailAddress = string.Empty;

        [ObservableProperty]
        private EmailProvider _selectedProvider = EmailProvider.Gmail;

        [ObservableProperty]
        private bool _isLoadingAccounts;
        #endregion

        public SettingsViewModel(IEmailAccountService emailAccountService)
        {
            _emailAccountService = emailAccountService;
        }

        #region Commands
        [RelayCommand]
        private async Task LoadAccountsAsync()
        {
            try
            {
                IsLoadingAccounts = true;
                await ExecuteAsync(async () =>
                {
                    var accounts = await _emailAccountService.GetAllAsync();
                    UpdateCollection(EmailAccounts, accounts);
                }, "Failed to load email accounts");
            }
            finally
            {
                IsLoadingAccounts = false;
            }
        }

        [RelayCommand]
        private void ShowNewAccountForm()
        {
            IsAccountFormVisible = true;
            AccountDisplayName = string.Empty;
            AccountEmailAddress = string.Empty;
            SelectedProvider = EmailProvider.Gmail;
            SelectedAccount = null;
        }

        [RelayCommand]
        private void ShowEditAccountForm(EmailAccountDto account)
        {
            if (account == null) return;

            SelectedAccount = account;
            IsAccountFormVisible = true;
            AccountDisplayName = account.DisplayName;
            AccountEmailAddress = account.EmailAddress;
            SelectedProvider = account.Provider;
        }

        [RelayCommand]
        private void CancelAccountForm()
        {
            IsAccountFormVisible = false;
            SelectedAccount = null;
            AccountDisplayName = string.Empty;
            AccountEmailAddress = string.Empty;
        }

        [RelayCommand]
        private async Task SaveAccountAsync()
        {
            if (string.IsNullOrWhiteSpace(AccountEmailAddress))
            {
                ErrorMessage = "Email address is required";
                return;
            }

            if (string.IsNullOrWhiteSpace(AccountDisplayName))
            {
                ErrorMessage = "Display name is required";
                return;
            }

            await ExecuteAsync(async () =>
            {
                if (SelectedAccount != null)
                {
                    
                    var updatedAccount = new EmailAccountDto(
                        SelectedAccount.Id,
                        AccountDisplayName,
                        AccountEmailAddress,
                        SelectedProvider,
                        SelectedAccount.AccessTokenExpiresUtc,
                        SelectedAccount.ConnectedAtUtc);

                    await _emailAccountService.UpdateAsync(updatedAccount);
                }
                else
                {
                   
                    var newAccount = new EmailAccountDto(
                        Guid.NewGuid(),
                        AccountDisplayName,
                        AccountEmailAddress,
                        SelectedProvider,
                        DateTime.UtcNow.AddHours(1), 
                        DateTime.UtcNow);

                    await _emailAccountService.AddAsync(newAccount);
                }

                IsAccountFormVisible = false;
                await LoadAccountsAsync();
            }, "Failed to save email account");
        }

        [RelayCommand]
        private async Task DeleteAccountAsync(EmailAccountDto account)
        {
            if (account == null) return;

            await ExecuteAsync(async () =>
            {
                await _emailAccountService.DeleteAsync(account.Id);
                if (SelectedAccount?.Id == account.Id)
                {
                    SelectedAccount = null;
                }
                await LoadAccountsAsync();
            }, "Failed to delete email account");
        }

        [RelayCommand]
        private void SelectAccount(EmailAccountDto account)
        {
            SelectedAccount = account;
        }
        #endregion

        public EmailProvider[] AvailableProviders => new[]
        {
            EmailProvider.Gmail,
            EmailProvider.Outlook
        };
    }
}

