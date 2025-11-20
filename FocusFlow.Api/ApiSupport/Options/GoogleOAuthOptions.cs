namespace FocusFlow.Api.ApiSupport.Options
{
    public sealed class GoogleOAuthOptions
    {
        public string ClientId { get; init; } = "";
        public string ClientSecret { get; init; } = "";
        public string RedirectUri { get; init; } = "";
    }
}
