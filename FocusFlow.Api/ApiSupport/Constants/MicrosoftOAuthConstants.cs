namespace FocusFlow.Api.ApiSupport.Constants
{
    public static class MicrosoftOAuthConstants
    {
        public const string AuthorizePath = "/authorize";
        public const string TokenPath = "/token";

        public const string UserInfoUrl = "https://graph.microsoft.com/v1.0/me";

        public const string GrantTypeAuthorizationCode = "authorization_code";
    }
}

