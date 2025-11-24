namespace FocusFlow.Api.ApiSupport.Options
{
    public sealed class MicrosoftOAuthOptions
    {
        public const string SectionName = "MicrosoftOAuth";

        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Tenant { get; set; } = "common";
        public string RedirectUri { get; set; } = string.Empty;

        public string[] Scopes { get; set; } =
        {
             "https://graph.microsoft.com/User.Read",
            "https://graph.microsoft.com/Mail.Read",
            "offline_access",
            "openid",
            "profile"
        };

        public string Authority =>
            $"https://login.microsoftonline.com/{Tenant}/oauth2/v2.0";
    }
}

