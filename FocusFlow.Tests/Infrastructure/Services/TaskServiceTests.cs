using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace FocusFlow.Tests.Infrastructure.Services;

public class TaskServiceTests : ServiceTestBase
{
    private readonly Mock<ITaskRepository> _repo = new();
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(_repo.Object);
    }

    [Fact]
    public async Task ReturnTaskIfTaskExists()
    {
        var taskId = Guid.NewGuid();
        var task = new FocusTask("Title", "Notes", DateTime.UtcNow);

        _repo.Setup(repository => repository.GetAsync(taskId, AnyCancellationToken)).ReturnsAsync(task);

        var result = await _service.GetAsync(taskId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Title");
    }

    [Fact]
    public async Task ReturnNullIfTaskDoesNotExist()
    {
        var taskId = Guid.NewGuid();

        _repo.Setup(repository => repository.GetAsync(taskId, AnyCancellationToken)).ReturnsAsync((FocusTask?)null);

        var result = await _service.GetAsync(taskId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateTaskAndReturnNewId()
    {
        var taskId = Guid.NewGuid();
        var dto = new FocusTaskDto(taskId, "Title", "Notes", DateTime.UtcNow, false, null);

        _repo.Setup(repository => repository.AddAsync(It.IsAny<FocusTask>(), AnyCancellationToken)).ReturnsAsync(taskId);

        var result = await _service.AddAsync(dto);

        result.Should().Be(taskId);
        _repo.Verify(repository => repository.AddAsync(It.Is<FocusTask>(task => task.Title == dto.Title), AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task SetTaskAsDoneWhenCreatingTaskWithIsDoneTrue()
    {
        var taskId = Guid.NewGuid();
        var dto = new FocusTaskDto(taskId, "Title", null, null, true, null);

        _repo.Setup(repository => repository.AddAsync(It.IsAny<FocusTask>(), AnyCancellationToken)).ReturnsAsync(taskId);

        await _service.AddAsync(dto);

        _repo.Verify(repository => repository.AddAsync(It.Is<FocusTask>(task => task.IsDone), AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskIfTaskExists()
    {
        var taskId = Guid.NewGuid();
        var task = new FocusTask("Old", "Old Notes", DateTime.UtcNow);
        var dto = new FocusTaskDto(taskId, "New", "New Notes", DateTime.UtcNow.AddDays(1), true, Guid.NewGuid());

        _repo.Setup(repository => repository.GetForUpdateAsync(taskId, AnyCancellationToken)).ReturnsAsync(task);

        await _service.UpdateAsync(dto);

        task.Title.Should().Be("New");
        task.Notes.Should().Be("New Notes");
        task.IsDone.Should().BeTrue();

        _repo.Verify(repository => repository.UpdateAsync(task, AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task ThrowExceptionIfUpdatingNonExistentTask()
    {
        var taskId = Guid.NewGuid();
        var dto = new FocusTaskDto(taskId, "Title", null, null, false, null);

        _repo.Setup(repository => repository.GetForUpdateAsync(taskId, AnyCancellationToken)).ReturnsAsync((FocusTask?)null);

        var action = async () => await _service.UpdateAsync(dto);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"Task with id {taskId} not found.");
    }

    [Fact]
    public async Task ReturnAllTasksWhenListingTasks()
    {
        var tasks = new[]
        {
            new FocusTask("Task 1"),
            new FocusTask("Task 2")
        };

        _repo.Setup(repository => repository.ListAsync(null, AnyCancellationToken)).ReturnsAsync(tasks.ToList());

        var result = await _service.ListAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ReturnOnlyDoneTasksIfFilteringByDoneTrue()
    {
        var tasks = new[]
        {
            new FocusTask("Done Task 1"),
            new FocusTask("Done Task 2")
        };
        tasks[0].Complete();
        tasks[1].Complete();

        _repo.Setup(repository => repository.ListAsync(true, AnyCancellationToken)).ReturnsAsync(tasks.ToList());

        var result = await _service.ListAsync(true);

        result.Should().HaveCount(2);
        result.All(taskItem => taskItem.IsDone).Should().BeTrue();
    }

    [Fact]
    public async Task ReturnOnlyUndoneTasksIfFilteringByDoneFalse()
    {
        var tasks = new[]
        {
            new FocusTask("Undone Task 1"),
            new FocusTask("Undone Task 2")
        };

        _repo.Setup(repository => repository.ListAsync(false, AnyCancellationToken)).ReturnsAsync(tasks.ToList());

        var result = await _service.ListAsync(false);

        result.Should().HaveCount(2);
        result.All(taskItem => !taskItem.IsDone).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTaskIfTaskExists()
    {
        var taskId = Guid.NewGuid();

        await _service.DeleteAsync(taskId);

        _repo.Verify(repository => repository.DeleteAsync(taskId, AnyCancellationToken), Times.Once);
    }
}