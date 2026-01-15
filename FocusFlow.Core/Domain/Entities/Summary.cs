using System;

namespace FocusFlow.Core.Domain.Entities
{
    public sealed class Summary
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid EmailId { get; private set; }
        public string Text { get; private set; } = "";
        public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

        private Summary() { }

        public Summary(Guid emailId, string text)
        {
            Id = Guid.NewGuid();
            EmailId = emailId;
            Text = text ?? "Geen samenvatting";
            CreatedUtc = DateTime.UtcNow;
        }

        public void UpdateText(string text)
        {
            Text = text ?? "";
            CreatedUtc = DateTime.UtcNow;
        }
    }
}
