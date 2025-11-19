using FocusFlow.Core.Domain.Enums;
using System;
using System.Collections.Generic;

namespace FocusFlow.Core.Domain.Entities
{
    public sealed class EmailAccount
    {
        public Guid Id { get; private set; } = Guid.NewGuid();


        public string DisplayName { get; private set; } = "";
        public string EmailAddress { get; private set; } = "";

        public EmailProvider Provider { get; private set; }

        public string AccessToken { get; private set; } = "";
        public string RefreshToken { get; private set; } = "";
        public DateTime AccessTokenExpiresUtc { get; private set; }

        public DateTime ConnectedAtUtc { get; private set; } = DateTime.UtcNow;

        public ICollection<Email> Emails { get; private set; } = new List<Email>();

        private EmailAccount() { }

        public EmailAccount(EmailProvider provider, string emailAddress, string displayName, string accessToken, string refreshToken, DateTime accessTokenExpiresUtc)
        {
            Provider = provider;
            EmailAddress = emailAddress ?? "";
            DisplayName = displayName ?? emailAddress ?? "";
            AccessToken = accessToken ?? "";
            RefreshToken = refreshToken ?? "";
            AccessTokenExpiresUtc = accessTokenExpiresUtc;
        }

        public void UpdateTokens(string accessToken, DateTime expiresAtUtc, string? refreshToken = null)
        {
            AccessToken = accessToken ?? "";
            AccessTokenExpiresUtc = expiresAtUtc;
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                RefreshToken = refreshToken!;
            }
        }
    }
}

