namespace FocusFlow.Core.Application.Utilities
{
    public static class GoogleOAuthConstants
    {
        public const string AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        public const string TokenUrl = "https://oauth2.googleapis.com/token";
        public const string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";
        public const string GrantTypeAuthorizationCode = "authorization_code";
        public const string GrantTypeRefreshToken = "refresh_token";

        public static class Scopes
        {
            public const string UserInfoEmail = "https://www.googleapis.com/auth/userinfo.email";
            public const string UserInfoProfile = "https://www.googleapis.com/auth/userinfo.profile";
            public const string GmailReadOnly = "https://www.googleapis.com/auth/gmail.readonly";

            public static readonly string[] All = new[]
            {
                UserInfoEmail,
                UserInfoProfile,
                GmailReadOnly
            };
        }
    }

    public static class MicrosoftOAuthConstants
    {
        public const string AuthorizePath = "/authorize";
        public const string TokenPath = "/token";
        public const string UserInfoUrl = "https://graph.microsoft.com/v1.0/me";
        public const string GrantTypeAuthorizationCode = "authorization_code";
        public const string GrantTypeRefreshToken = "refresh_token";
    }
}