using FocusFlow.Core.Domain.Entities;
using FluentAssertions;

namespace FocusFlow.Tests.Domain.Entities;

public class ReminderTests
{
    [Fact]
    public void CreateReminderWithAllProperties()
    {
        var reminder = new Reminder("Title", DateTime.UtcNow.AddDays(1), Guid.NewGuid(), Guid.NewGuid());

        reminder.Title.Should().Be("Title");
        reminder.Fired.Should().BeFalse();
        reminder.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void SetEmptyStringWhenCreatingReminderWithNullTitle()
    {
        var reminder = new Reminder(null!, DateTime.UtcNow);
        reminder.Title.Should().BeEmpty();
    }

    [Fact]
    public void SetFiredToTrueWhenMarkingReminderAsFired()
    {
        var reminder = new Reminder("Title", DateTime.UtcNow);
        reminder.MarkFired();
        reminder.Fired.Should().BeTrue();
    }
}
