using System;

namespace FocusFlow.Core.Domain.Entities
{
    public sealed class Email
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string From { get; private set; } = "";
        public string Subject { get; private set; } = "";
        public string BodyText { get; private set; } = "";
        public DateTime ReceivedUtc { get; private set; }
        public int PriorityScore { get; private set; }

        private Email() { }

        public Email(string from, string subject, string body, DateTime receivedUtc)
        {
            From = from ?? ""; Subject = subject ?? ""; BodyText = body ?? "";
            ReceivedUtc = receivedUtc; PriorityScore = 0;
        }

        public void SetPriority(int score) => PriorityScore = Math.Clamp(score, 0, 100);

        public void Update(string from, string subject, string bodyText, DateTime receivedUtc, int priorityScore)
        {
            From = from ?? "";
            Subject = subject ?? "";
            BodyText = bodyText ?? "";
            ReceivedUtc = receivedUtc;
            SetPriority(priorityScore);
        }
    }
}
