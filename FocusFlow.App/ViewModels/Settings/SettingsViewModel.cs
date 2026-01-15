using System;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Enums;

using Microsoft.Extensions.Configuration;

namespace FocusFlow.App.ViewModels.Settings
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly IEmailAccountService _emailAccountService;
        private readonly IConfiguration _configuration;

        public SettingsViewModel(IEmailAccountService emailAccountService, IConfiguration configuration)
        {
            _emailAccountService = emailAccountService;
            _configuration = configuration;
        }

        public EmailProvider[] AvailableProviders => new[]
        {
            EmailProvider.Gmail,
            EmailProvider.Outlook
        };

        public string GetTokenStatus(EmailAccountDto account)
        {
            if (account == null) return "Unknown";

            var timeUntilExpiry = account.AccessTokenExpiresUtc - DateTime.UtcNow;

            if (timeUntilExpiry <= TimeSpan.Zero)
                return "Expired";

            if (timeUntilExpiry <= TimeSpan.FromMinutes(15))
                return "Expiring soon";

            if (timeUntilExpiry <= TimeSpan.FromHours(1))
                return $"Expires in {timeUntilExpiry.Minutes} min";

            return $"Valid (expires {account.AccessTokenExpiresUtc:dd/MM/yyyy HH:mm})";
        }
    }
}
