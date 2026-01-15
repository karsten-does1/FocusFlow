using FocusFlow.Core.Domain.Entities;
using FluentAssertions;

namespace FocusFlow.Tests.Domain.Entities;

public class FocusTaskTests
{
    [Fact]
    public void CreateTaskWithAllProperties()
    {
        var task = new FocusTask("Title", "Notes", DateTime.UtcNow, Guid.NewGuid());

        task.Title.Should().Be("Title");
        task.Notes.Should().Be("Notes");
        task.IsDone.Should().BeFalse();
        task.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void SetEmptyStringWhenCreatingTaskWithNullTitle()
    {
        var task = new FocusTask(null!);

        task.Title.Should().BeEmpty();
    }

    [Fact]
    public void SetIsDoneToTrueWhenCompletingTask()
    {
        var task = new FocusTask("Task");
        task.Complete();
        task.IsDone.Should().BeTrue();
    }

    [Fact]
    public void SetIsDoneToFalseWhenReopeningTask()
    {
        var task = new FocusTask("Task");
        task.Complete();
        task.Reopen();
        task.IsDone.Should().BeFalse();
    }

    [Fact]
    public void UpdateAllPropertiesWhenUpdatingTask()
    {
        var task = new FocusTask("Old", "Old Notes", DateTime.UtcNow);
        task.Update("New", "New Notes", DateTime.UtcNow.AddDays(1), true, Guid.NewGuid());

        task.Title.Should().Be("New");
        task.Notes.Should().Be("New Notes");
        task.IsDone.Should().BeTrue();
    }

    [Fact]
    public void SetEmptyStringWhenUpdatingTaskWithNullTitle()
    {
        var task = new FocusTask("Title");
        task.Update(null!, null, null, null, null);
        task.Title.Should().BeEmpty();
    }

    [Fact]
    public void ReopenTaskWhenUpdatingWithIsDoneFalse()
    {
        var task = new FocusTask("Task");
        task.Complete();
        task.Update("New", null, null, false, null);
        task.IsDone.Should().BeFalse();
    }

    [Fact]
    public void KeepCurrentStatusWhenUpdatingTaskWithoutIsDone()
    {
        var task = new FocusTask("Task");
        task.Complete();
        task.Update("New", null, null, null, null);
        task.IsDone.Should().BeTrue();
    }
}
