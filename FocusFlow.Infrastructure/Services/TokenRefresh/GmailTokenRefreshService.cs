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
    public sealed class GmailTokenRefreshService : ITokenRefreshService
    {
        private readonly FocusFlowDbContext _db;
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ILogger<GmailTokenRefreshService> _logger;

        public GmailTokenRefreshService(
            FocusFlowDbContext db,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IEmailAccountService emailAccountService,
            ILogger<GmailTokenRefreshService> logger)
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
                    .FirstOrDefaultAsync(a => a.Id == accountId && a.Provider == EmailProvider.Gmail, ct);

                if (account == null)
                    return new TokenRefreshResult(false, ErrorMessage: $"Account {accountId} not found");

                if (string.IsNullOrWhiteSpace(account.RefreshToken))
                    return new TokenRefreshResult(false, ErrorMessage: "No refresh token available");

                var clientId = _configuration["GoogleOAuth:ClientId"];
                var clientSecret = _configuration["GoogleOAuth:ClientSecret"];

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                    return new TokenRefreshResult(false, ErrorMessage: "Google OAuth clientId/clientSecret not configured");

                using var request = new HttpRequestMessage(HttpMethod.Post, GoogleOAuthConstants.TokenUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["refresh_token"] = account.RefreshToken,
                        ["grant_type"] = GoogleOAuthConstants.GrantTypeRefreshToken
                    })
                };

                var response = await _http.SendAsync(request, ct);

                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to refresh Gmail token. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseBody);

                    return new TokenRefreshResult(false, ErrorMessage: $"Token refresh failed: {response.StatusCode}");
                }

                using var json = JsonDocument.Parse(responseBody);
                var root = json.RootElement;

                string? newAccessToken = null;
                int expiresIn = 0;

                if (root.TryGetProperty("access_token", out var atEl))
                    newAccessToken = atEl.GetString();

                if (root.TryGetProperty("expires_in", out var expEl) && expEl.TryGetInt32(out var exp))
                    expiresIn = exp;

                if (string.IsNullOrWhiteSpace(newAccessToken) || expiresIn <= 0)
                {
                    _logger.LogError("Unexpected Gmail token refresh response: {Response}", responseBody);
                    return new TokenRefreshResult(false, ErrorMessage: "Invalid token refresh response");
                }

                // clock-skew marge: 60s
                var safeExpiresIn = Math.Max(0, expiresIn - 60);
                var newExpiresAtUtc = DateTime.UtcNow.AddSeconds(safeExpiresIn);

                await _emailAccountService.UpdateTokensAsync(
                    accountId,
                    newAccessToken,
                    newExpiresAtUtc,
                    refreshToken: null,
                    ct: ct);

                _logger.LogInformation("Successfully refreshed Gmail token for account {AccountId}", accountId);

                return new TokenRefreshResult(true, newAccessToken, newExpiresAtUtc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Gmail token for account {AccountId}", accountId);
                return new TokenRefreshResult(false, ErrorMessage: ex.Message);
            }
        }

        public async Task<bool> IsTokenExpiredAsync(Guid accountId, CancellationToken ct = default)
        {
            var account = await _db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == accountId && a.Provider == EmailProvider.Gmail, ct);

            if (account == null) return true;

            return DateTime.UtcNow >= account.AccessTokenExpiresUtc;
        }

        public async Task<bool> ShouldRefreshTokenAsync(Guid accountId, CancellationToken ct = default)
        {
            var account = await _db.EmailAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == accountId && a.Provider == EmailProvider.Gmail, ct);

            if (account == null) return false;
            if (string.IsNullOrWhiteSpace(account.RefreshToken)) return false;

            var timeUntilExpiry = account.AccessTokenExpiresUtc - DateTime.UtcNow;
            return timeUntilExpiry <= TimeSpan.FromMinutes(15);
        }
    }
}
