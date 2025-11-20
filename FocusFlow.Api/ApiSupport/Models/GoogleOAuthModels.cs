using System;

namespace FocusFlow.Api.ApiSupport.Models
{
   
    public sealed record GoogleTokenResponse(
        string AccessToken,
        string? RefreshToken,
        DateTime ExpiresAtUtc);

    public sealed record GoogleUserInfo(
        string Email,
        string DisplayName);
}

