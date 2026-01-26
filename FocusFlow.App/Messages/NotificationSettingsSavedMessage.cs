namespace FocusFlow.App.Messages
{
    public sealed class NotificationSettingsSavedMessage
    {
        public static NotificationSettingsSavedMessage Instance { get; } = new();

        private NotificationSettingsSavedMessage() { }
    }
}
