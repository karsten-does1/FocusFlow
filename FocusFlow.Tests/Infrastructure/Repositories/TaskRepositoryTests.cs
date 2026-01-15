using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Repositories;
using FluentAssertions;

namespace FocusFlow.Tests.Infrastructure.Repositories;

public class TaskRepositoryTests : RepositoryTestBase
{
    private readonly TaskRepository _repository;

    public TaskRepositoryTests()
    {
        _repository = new TaskRepository(DbContext);
    }

    [Fact]
    public async Task AddTaskReturnId()
    {
        var task = new FocusTask("Test Task", "Notes", DateTime.UtcNow);

        var result = await _repository.AddAsync(task);

        result.Should().Be(task.Id);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTaskIfTaskExists()
    {
        var task = await AddToDatabaseAsync(new FocusTask("Test Task", "Notes", DateTime.UtcNow));

        var result = await _repository.GetAsync(task.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
        result.Notes.Should().Be("Notes");
    }

    [Fact]
    public async Task ReturnNullIfTaskDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _repository.GetAsync(nonExistentId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTaskForUpdateIfTaskExists()
    {
        var task = await AddToDatabaseAsync(new FocusTask("Test Task", "Notes", DateTime.UtcNow));

        var result = await _repository.GetForUpdateAsync(task.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
    }

    [Fact]
    public async Task ListAllTasksIfNoFilterProvided()
    {
        await AddRangeToDatabaseAsync(new[]
        {
            new FocusTask("Task 1", null, DateTime.UtcNow),
            new FocusTask("Task 2", null, DateTime.UtcNow)
        });

        var result = await _repository.ListAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListOnlyDoneTasksIfFilteringByDoneTrue()
    {
        var doneTask = new FocusTask("Done Task", null, DateTime.UtcNow);
        doneTask.Complete();
        await AddRangeToDatabaseAsync(new[]
        {
            doneTask,
            new FocusTask("Undone Task", null, DateTime.UtcNow)
        });

        var result = await _repository.ListAsync(true);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Done Task");
        result[0].IsDone.Should().BeTrue();
    }

    [Fact]
    public async Task ListOnlyUndoneTasksIfFilteringByDoneFalse()
    {
        var doneTask = new FocusTask("Done Task", null, DateTime.UtcNow);
        doneTask.Complete();
        await AddRangeToDatabaseAsync(new[]
        {
            doneTask,
            new FocusTask("Undone Task", null, DateTime.UtcNow)
        });

        var result = await _repository.ListAsync(false);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Undone Task");
        result[0].IsDone.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTaskIfTaskExists()
    {
        var task = await AddToDatabaseAsync(new FocusTask("Old Title", "Old Notes", DateTime.UtcNow));
        task.Update("New Title", "New Notes", DateTime.UtcNow.AddDays(1), true, null);

        await _repository.UpdateAsync(task);

        var updatedTask = await _repository.GetAsync(task.Id);
        updatedTask.Should().NotBeNull();
        updatedTask!.Title.Should().Be("New Title");
        updatedTask.Notes.Should().Be("New Notes");
        updatedTask.IsDone.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTaskIfTaskExists()
    {
        var task = await AddToDatabaseAsync(new FocusTask("Test Task", null, DateTime.UtcNow));

        await _repository.DeleteAsync(task.Id);

        var result = await _repository.GetAsync(task.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotThrowDeletingNonExistentTask()
    {
        var nonExistentId = Guid.NewGuid();

        var action = async () => await _repository.DeleteAsync(nonExistentId);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OrderTasksByDoneStatusThenByDueDate()
    {
        var task2 = new FocusTask("Task 2", null, DateTime.UtcNow.AddDays(1));
        task2.Complete();
        await AddRangeToDatabaseAsync(new[]
        {
            new FocusTask("Task 1", null, DateTime.UtcNow.AddDays(3)),
            task2,
            new FocusTask("Task 3", null, DateTime.UtcNow.AddDays(2))
        });

        var result = await _repository.ListAsync();

        result.Should().HaveCount(3);
        result[0].Title.Should().Be("Task 3");
        result[1].Title.Should().Be("Task 1");
        result[2].Title.Should().Be("Task 2");
    }
}
