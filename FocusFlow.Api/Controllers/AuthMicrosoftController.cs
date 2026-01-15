using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Utilities;
using FocusFlow.Api.ApiSupport.Models;
using FocusFlow.Api.ApiSupport.Options;
using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FocusFlow.Api.Controllers
{
    [ApiController]
    [Route("api/auth/microsoft")]
    public sealed class AuthMicrosoftController : ControllerBase
    {
        private readonly MicrosoftOAuthOptions _options;
        private readonly IEmailAccountService _emailAccounts;
        private readonly HttpClient _http;
        private readonly ILogger<AuthMicrosoftController> _logger;

        public AuthMicrosoftController(
            IOptions<MicrosoftOAuthOptions> options,
            IEmailAccountService emailAccounts,
            IHttpClientFactory httpClientFactory,
            ILogger<AuthMicrosoftController> logger)
        {
            _options = options.Value;
            _emailAccounts = emailAccounts;
            _http = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var scopesString = string.Join(" ", _options.Scopes);

            var authorizeUrl =
                $"{_options.Authority}{MicrosoftOAuthConstants.AuthorizePath}" +
                $"?client_id={Uri.EscapeDataString(_options.ClientId)}" +
                $"&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
                $"&response_mode=query" +
                $"&scope={Uri.EscapeDataString(scopesString)}" +
                $"&state={Guid.NewGuid():N}" +
                $"&prompt=select_account";

            _logger.LogInformation("Redirecting user to Microsoft OAuth login");
            return Redirect(authorizeUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(
            [FromQuery] string? code,
            [FromQuery] string? error,
            CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("Microsoft OAuth returned error: {Error}", error);
                return BadRequest(new { error });
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Microsoft callback called without authorization code");
                return BadRequest("Authorization code is required");
            }

            try
            {
                var tokens = await ExchangeCodeForTokensAsync(code, ct);
                if (tokens == null)
                {
                    _logger.LogError("Failed to exchange Microsoft code for tokens");
                    return StatusCode(500, "Failed to obtain tokens from Microsoft");
                }

                var user = await FetchUserInfoAsync(tokens.AccessToken, ct);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogError("Failed to fetch Microsoft user info");
                    return StatusCode(500, "Failed to fetch Microsoft user information");
                }

                var accountId = await CreateOrUpdateEmailAccountAsync(user, tokens, ct);

                await _emailAccounts.UpdateTokensAsync(
                    accountId,
                    tokens.AccessToken,
                    tokens.ExpiresAtUtc,
                    tokens.RefreshToken,
                    ct);

                _logger.LogInformation("Successfully connected Microsoft/Outlook account: {Email}", user.Email);

                return Content($"Outlook-account {user.Email} is gekoppeld. Je kunt dit venster sluiten.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Microsoft OAuth callback");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private async Task<MicrosoftTokenResponse?> ExchangeCodeForTokensAsync(
            string code,
            CancellationToken ct)
        {
            var tokenUrl = $"{_options.Authority}{MicrosoftOAuthConstants.TokenPath}";

            var body = new StringBuilder();
            body.Append($"client_id={Uri.EscapeDataString(_options.ClientId)}");
            body.Append($"&scope={Uri.EscapeDataString(string.Join(" ", _options.Scopes))}");
            body.Append($"&grant_type={MicrosoftOAuthConstants.GrantTypeAuthorizationCode}");
            body.Append($"&code={Uri.EscapeDataString(code)}");
            body.Append($"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}");
            body.Append($"&client_secret={Uri.EscapeDataString(_options.ClientSecret)}");

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new StringContent(body.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Failed to exchange Microsoft code for tokens. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode,
                    errorContent);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var raw = await JsonSerializer.DeserializeAsync<MicrosoftTokenResponseRaw>(stream, cancellationToken: ct);

            if (raw == null || string.IsNullOrWhiteSpace(raw.access_token) || raw.expires_in <= 0)
            {
                _logger.LogError("Microsoft token response was empty or missing access_token/expires_in");
                return null;
            }

            // clock-skew marge: 60s
            var safeExpiresIn = Math.Max(0, raw.expires_in - 60);
            var expiresAt = DateTime.UtcNow.AddSeconds(safeExpiresIn);

            return new MicrosoftTokenResponse(
                raw.access_token,
                raw.refresh_token,
                expiresAt);
        }

        private async Task<MicrosoftUserInfo?> FetchUserInfoAsync(
            string accessToken,
            CancellationToken ct)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                MicrosoftOAuthConstants.UserInfoUrl);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Failed to fetch Microsoft user info. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode,
                    errorContent);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var user = await JsonSerializer.DeserializeAsync<MicrosoftUserInfo>(stream, cancellationToken: ct);

            return user;
        }

        private async Task<Guid> CreateOrUpdateEmailAccountAsync(
            MicrosoftUserInfo userInfo,
            MicrosoftTokenResponse tokens,
            CancellationToken ct)
        {
            var email = userInfo.Email;

            var existingAccount = await _emailAccounts.GetByEmailAddressAsync(email, ct);

            if (existingAccount == null)
            {
                var displayName = !string.IsNullOrWhiteSpace(userInfo.displayName)
                    ? userInfo.displayName
                    : email;

                var newAccountDto = new EmailAccountDto(
                    Id: Guid.Empty,
                    DisplayName: displayName,
                    EmailAddress: email,
                    Provider: EmailProvider.Outlook,
                    AccessTokenExpiresUtc: tokens.ExpiresAtUtc,
                    ConnectedAtUtc: DateTime.UtcNow);

                var accountId = await _emailAccounts.AddAsync(newAccountDto, ct);
                _logger.LogInformation("Created new Outlook email account for {Email}", email);
                return accountId;
            }
            else
            {
                _logger.LogInformation("Updating existing Outlook email account for {Email}", email);
                return existingAccount.Id;
            }
        }
    }
}
