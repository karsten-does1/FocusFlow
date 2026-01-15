using System;

namespace FocusFlow.Core.Domain.Entities
{
    public sealed class Reminder
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Title { get; private set; } = "";
        public DateTime FireAtUtc { get; private set; }
        public bool Fired { get; private set; }
        public Guid? RelatedTaskId { get; private set; }
        public Guid? RelatedEmailId { get; private set; }

        private Reminder() { }

        public Reminder(string title, DateTime fireAtUtc, Guid? relatedTaskId = null, Guid? relatedEmailId = null)
        {
            Title = title ?? "";
            FireAtUtc = fireAtUtc;
            RelatedTaskId = relatedTaskId;
            RelatedEmailId = relatedEmailId;
            Fired = false;
        }

        public void MarkFired() => Fired = true;
    }
}