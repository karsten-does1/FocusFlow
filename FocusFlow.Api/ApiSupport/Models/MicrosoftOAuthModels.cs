namespace FocusFlow.Api.ApiSupport.Models
{
    public sealed class MicrosoftTokenResponseRaw
    {
        public string token_type { get; set; } = "";
        public int expires_in { get; set; }                
        public string scope { get; set; } = "";
        public string access_token { get; set; } = "";
        public string? refresh_token { get; set; }
    }

    public sealed record MicrosoftTokenResponse(
        string AccessToken,
        string? RefreshToken,
        DateTime ExpiresAtUtc);

    public sealed class MicrosoftUserInfo
    {
        public string? mail { get; set; }                 
        public string? userPrincipalName { get; set; }    
        public string? displayName { get; set; }

        public string Email =>
            string.IsNullOrWhiteSpace(mail)
                ? (userPrincipalName ?? string.Empty)
                : mail;
    }
}
