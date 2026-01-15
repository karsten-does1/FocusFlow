using FocusFlow.Core.Application.Utilities;
using FocusFlow.Api.ApiSupport.Models;
using FocusFlow.Api.ApiSupport.Options;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/auth/google")]
    public sealed class AuthGoogleController : ControllerBase
    {
        private readonly GoogleOAuthOptions _options;
        private readonly IEmailAccountService _emailAccounts;
        private readonly HttpClient _http;
        private readonly ILogger<AuthGoogleController> _logger;

        public AuthGoogleController(
            IOptions<GoogleOAuthOptions> options,
            IEmailAccountService emailAccounts,
            IHttpClientFactory httpClientFactory,
            ILogger<AuthGoogleController> logger)
        {
            _options = options.Value;
            _emailAccounts = emailAccounts;
            _http = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var redirectUrl = BuildGoogleAuthUrl(GoogleOAuthConstants.Scopes.All);
            _logger.LogInformation("Redirecting user to Google OAuth login");

            return Redirect(redirectUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Callback called without authorization code");
                return BadRequest("Authorization code is required");
            }

            try
            {
                var tokens = await ExchangeCodeForTokensAsync(code, ct);
                if (tokens == null)
                {
                    return StatusCode(500, "Failed to obtain tokens from Google");
                }

                var userInfo = await FetchUserInfoAsync(tokens.AccessToken, ct);
                if (userInfo == null)
                {
                    return StatusCode(500, "Failed to fetch user information");
                }

                var accountId = await CreateOrUpdateEmailAccountAsync(userInfo, tokens, ct);

                await _emailAccounts.UpdateTokensAsync(
                    accountId,
                    tokens.AccessToken,
                    tokens.ExpiresAtUtc,
                    tokens.RefreshToken,
                    ct);

                _logger.LogInformation("Successfully connected Gmail account: {Email}", userInfo.Email);

                return Content($"Gmail-account {userInfo.Email} is gekoppeld. Je kunt dit venster sluiten.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OAuth callback");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private string BuildGoogleAuthUrl(string[] scopes)
        {
            var scopesString = string.Join(' ', scopes);
            return $"{GoogleOAuthConstants.AuthorizationUrl}" +
                   $"?response_type=code" +
                   $"&client_id={Uri.EscapeDataString(_options.ClientId)}" +
                   $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                   $"&scope={Uri.EscapeDataString(scopesString)}" +
                   $"&access_type=offline" +
                   $"&prompt=consent";
        }

        private async Task<GoogleTokenResponse?> ExchangeCodeForTokensAsync(string code, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, GoogleOAuthConstants.TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["redirect_uri"] = _options.RedirectUri,
                    ["grant_type"] = GoogleOAuthConstants.GrantTypeAuthorizationCode
                })
            };

            var response = await _http.SendAsync(request, ct);

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to exchange code for tokens. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseBody);
                return null;
            }

            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            string? accessToken = null;
            int expiresIn = 0;

            if (root.TryGetProperty("access_token", out var atEl))
                accessToken = atEl.GetString();

            if (root.TryGetProperty("expires_in", out var expEl) && expEl.TryGetInt32(out var exp))
                expiresIn = exp;

            var refreshToken = root.TryGetProperty("refresh_token", out var rtEl)
                ? rtEl.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(accessToken) || expiresIn <= 0)
            {
                _logger.LogError("Unexpected Google token response: {Response}", responseBody);
                return null;
            }

            // clock-skew marge: 60s
            var safeExpiresIn = Math.Max(0, expiresIn - 60);
            var expiresAtUtc = DateTime.UtcNow.AddSeconds(safeExpiresIn);

            return new GoogleTokenResponse(accessToken, refreshToken, expiresAtUtc);
        }

        private async Task<GoogleUserInfo?> FetchUserInfoAsync(string accessToken, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, GoogleOAuthConstants.UserInfoUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _http.SendAsync(request, ct);

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch user info. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseBody);
                return null;
            }

            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            // email is normaal aanwezig, maar we doen dit defensief
            string email = "";
            if (root.TryGetProperty("email", out var emailEl))
                email = emailEl.GetString() ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogError("Google user info response missing email: {Response}", responseBody);
                return null;
            }

            var displayName = ExtractDisplayName(root, email);
            return new GoogleUserInfo(email, displayName);
        }

        private static string ExtractDisplayName(JsonElement userInfoRoot, string fallbackEmail)
        {
            if (userInfoRoot.TryGetProperty("name", out var nameProperty))
            {
                var name = nameProperty.GetString();
                return !string.IsNullOrWhiteSpace(name) ? name : fallbackEmail;
            }

            return fallbackEmail;
        }

        private async Task<Guid> CreateOrUpdateEmailAccountAsync(
            GoogleUserInfo userInfo,
            GoogleTokenResponse tokens,
            CancellationToken ct)
        {
            var existingAccount = await _emailAccounts.GetByEmailAddressAsync(userInfo.Email, ct);

            if (existingAccount == null)
            {
                var newAccountDto = new EmailAccountDto(
                    Id: Guid.Empty,
                    DisplayName: userInfo.DisplayName,
                    EmailAddress: userInfo.Email,
                    Provider: EmailProvider.Gmail,
                    AccessTokenExpiresUtc: tokens.ExpiresAtUtc,
                    ConnectedAtUtc: DateTime.UtcNow);

                var accountId = await _emailAccounts.AddAsync(newAccountDto, ct);
                _logger.LogInformation("Created new email account for {Email}", userInfo.Email);
                return accountId;
            }
            else
            {
                _logger.LogInformation("Updating existing email account for {Email}", userInfo.Email);
                return existingAccount.Id;
            }
        }
    }
}
