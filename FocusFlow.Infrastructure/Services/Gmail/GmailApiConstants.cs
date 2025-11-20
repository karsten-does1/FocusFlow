namespace FocusFlow.Infrastructure.Services.Gmail
{
    public static class GmailApiConstants
    {
        public const string MessagesEndpoint = "gmail/v1/users/me/messages";
        public const int DefaultMaxCount = 20;
        public const int MaxMaxCount = 500;
        public const int MinMaxCount = 1;

        public const string DefaultSearchQuery =
            "in:inbox -in:spam -in:trash -category:promotions -category:social -category:forums -category:updates";
    }
}

