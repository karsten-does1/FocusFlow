namespace FocusFlow.App.Messages
{
    public sealed class DailyBriefingDueMessage
    {
        public static DailyBriefingDueMessage Instance { get; } = new();
        private DailyBriefingDueMessage() { }
    }
}
