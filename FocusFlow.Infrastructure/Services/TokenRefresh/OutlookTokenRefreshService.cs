using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Application.Utilities;
using FocusFlow.Core.Domain.Enums;
using FocusFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FocusFlow.Infrastructure.Services.TokenRefresh
{
    public sealed class OutlookTokenRefreshService : ITokenRefreshService
    {
        private readonly FocusFlowDbContext _db;
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ILogger<OutlookTokenRefreshService> _logger;

        public OutlookTokenRefreshService(
            FocusFlowDbContext db,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IEmailAccountService emailAccountService,
            ILogger<OutlookTokenRefreshService> logger)
        {
            _db = db;
            _http = httpClientFactory.CreateClient();
            _configuration = configuration;
            _emailAccountService = emailAccountService;
            _logger = logger;
        }

        public async Task<TokenRefreshResult> RefreshTokenAsync(Guid accountId, CancellationToken ct = default)
        {
            try
            {
                var account = await _db.EmailAccounts
                    .FirstOrDefaultAsync(a => a.Id == accountId && a.Provider == EmailProvider.Outlook, ct);

                if (account == null)
                    return new TokenRefreshResult(false, ErrorMessage: $"Account {accountId} not found");

                if (string.IsNullOrWhiteSpace(account.RefreshToken))
                    return new TokenRefreshResult(false, ErrorMessage: "No refresh token available");

                var tenant = _configuration["MicrosoftOAuth:Tenant"] ?? "common";
                var authority = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0";
                var tokenUrl = $"{authority}{MicrosoftOAuthConstants.TokenPath}";

                var clientId = _configuration["MicrosoftOAuth:ClientId"];
                var clientSecret = _configuration["MicrosoftOAuth:ClientSecret"];

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                    return new TokenRefreshResult(false, ErrorMessage: "Microsoft OAuth clientId/clientSecret not configured");

                var scopes = _configuration.GetSection("MicrosoftOAuth:Scopes").Get<string[]>()
                    ?? new[] { "https://graph.microsoft.com/Mail.Read", "offline_access", "openid", "profile" };

                using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["grant_type"] = MicrosoftOAuthConstants.GrantTypeRefreshToken,
                        ["refresh_token"] = account.RefreshToken,
                        ["scope"] = string.Join(" ", scopes)
                    })
                };

                var response = await _http.SendAsync(request, ct);

                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to refresh Outlook token. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseBody);

                    return new TokenRefreshResult(false, ErrorMessage: $"Token refresh failed: {response.StatusCode}");
                }

                var raw = JsonSerializer.Deserialize<MicrosoftTokenResponseRaw>(responseBody);

                if (raw == null || string.IsNullOrWhiteSpace(raw.access_token) || raw.expires_in <= 0)
                {
                    _logger.LogError("Unexpected Outlook token refresh response: {Response}", responseBody);
                    return new TokenRefreshResult(false, ErrorMessage: "Invalid token refresh response");
                }

                // clock-skew marge: 60s
                var safeExpiresIn = Math.Max(0, raw.expires_in - 60);
                var newExpiresAtUtc = DateTime.UtcNow.AddSeconds(safeExpiresIn);

                var newRefreshToken = !string.IsNullOrWhiteSpace(raw.refresh_token)
                    ? raw.refresh_token
                    : account.RefreshToken;

                await _emailAccountService.UpdateTokensAsync(
                    accountId,
                    raw.access_token,
                    newExpiresAtUtc,
                    newRefreshToken,
                    ct);

                _logger.LogInformation("Successfully refreshed Outlook token for account {AccountId}", accountId);

                return new TokenRefreshResult(true, raw.access_token, newExpiresAtUtc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Outlook token for account {AccountId}", accountId);
                return new TokenRefreshResult(false, ErrorMessage: ex.Message);
            }
        }

        public async Task<bool> IsTokenExpiredAsync(Guid accountId, CancellationToken ct = default)
        {
            var account = await _db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == accountId && a.Provider == EmailProvider.Outlook, ct);

            if (account == null) return true;

            return DateTime.UtcNow >= account.AccessTokenExpiresUtc;
        }

        public async Task<bool> ShouldRefreshTokenAsync(Guid accountId, CancellationToken ct = default)
        {
            var account = await _db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == accountId && a.Provider == EmailProvider.Outlook, ct);

            if (account == null) return false;
            if (string.IsNullOrWhiteSpace(account.RefreshToken)) return false;

            var timeUntilExpiry = account.AccessTokenExpiresUtc - DateTime.UtcNow;
            return timeUntilExpiry <= TimeSpan.FromMinutes(15);
        }

        private sealed class MicrosoftTokenResponseRaw
        {
            public string token_type { get; set; } = "";
            public int expires_in { get; set; }
            public string scope { get; set; } = "";
            public string access_token { get; set; } = "";
            public string? refresh_token { get; set; }
        }
    }
}
