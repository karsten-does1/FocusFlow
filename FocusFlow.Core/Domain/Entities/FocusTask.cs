using System;

namespace FocusFlow.Core.Domain.Entities
{
    public sealed class FocusTask
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Title { get; private set; } = "";
        public string? Notes { get; private set; }
        public DateTime? DueUtc { get; private set; }
        public bool IsDone { get; private set; }
        public Guid? RelatedEmailId { get; private set; }

        private FocusTask() { }

        public FocusTask(string title, string? notes = null, DateTime? dueUtc = null, Guid? relatedEmailId = null)
        {
            Title = title ?? "";
            Notes = notes;
            DueUtc = dueUtc;
            RelatedEmailId = relatedEmailId;
            IsDone = false;
        }

        public void Complete() => IsDone = true;
        public void Reopen() => IsDone = false;
        public void Update(string title, string? notes, DateTime? dueUtc)
        {
            Title = title ?? "";
            Notes = notes;
            DueUtc = dueUtc;
        }
    }
}

