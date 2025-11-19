using FocusFlow.Core.Domain.Enums;
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

        public EmailProvider Provider { get; private set; } = EmailProvider.Unknown;
        public string? ExternalMessageId { get; private set; }
        public Guid? EmailAccountId { get; private set; }
        public EmailAccount? EmailAccount { get; private set; }


        private Email() { }

        public Email(string from, string subject, string body, DateTime receivedUtc, EmailProvider provider = EmailProvider.Unknown, string? externalMessageId = null, Guid? emailAccountId = null)
        {
            From = from ?? "";
            Subject = subject ?? "";
            BodyText = body ?? "";
            ReceivedUtc = receivedUtc;
            PriorityScore = 0;
            Provider = provider;
            ExternalMessageId = externalMessageId;
            EmailAccountId = emailAccountId;
        }

        public void SetPriority(int score) => PriorityScore = Math.Clamp(score, 0, 100);

        public void Update(string from, string subject, string bodyText, DateTime receivedUtc, int priorityScore, EmailProvider? provider = null, string? externalMessageId = null, Guid? emailAccountId = null)
        {
            From = from ?? "";
            Subject = subject ?? "";
            BodyText = bodyText ?? "";
            ReceivedUtc = receivedUtc;
            SetPriority(priorityScore);

            if (provider.HasValue)
                Provider = provider.Value;
            if (externalMessageId != null)
                ExternalMessageId = externalMessageId;
            if (emailAccountId.HasValue)
                EmailAccountId = emailAccountId;
        }

        public void LinkToAccount(EmailProvider provider, Guid accountId, string externalMessageId)
        {
            Provider = provider;
            EmailAccountId = accountId;
            ExternalMessageId = externalMessageId;
        }
    }
}
