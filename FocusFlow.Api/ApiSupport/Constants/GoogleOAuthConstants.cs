namespace FocusFlow.Api.ApiSupport.Constants
{
  
    public static class GoogleOAuthConstants
    {
        public const string AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        public const string TokenUrl = "https://oauth2.googleapis.com/token";
        public const string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";
        public const string GrantTypeAuthorizationCode = "authorization_code";

        // Required OAuth scopes
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
}

