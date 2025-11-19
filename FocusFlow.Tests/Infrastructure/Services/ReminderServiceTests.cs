using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace FocusFlow.Tests.Infrastructure.Services;

public class ReminderServiceTests : ServiceTestBase
{
    private readonly Mock<IReminderRepository> _repo = new();
    private readonly ReminderService _service;

    public ReminderServiceTests()
    {
        _service = new ReminderService(_repo.Object);
    }

    [Fact]
    public async Task ReturnReminderIfReminderExists()
    {
        var reminderId = Guid.NewGuid();
        var reminder = new Reminder("Title", DateTime.UtcNow);

        _repo.Setup(repository => repository.GetAsync(reminderId, AnyCancellationToken)).ReturnsAsync(reminder);

        var result = await _service.GetAsync(reminderId);

        result.Should().NotBeNull();
        result!.Title.Should().Be(reminder.Title);
    }

    [Fact]
    public async Task ReturnNullIfReminderDoesNotExist()
    {
        var reminderId = Guid.NewGuid();

        _repo.Setup(repository => repository.GetAsync(reminderId, AnyCancellationToken)).ReturnsAsync((Reminder?)null);

        var result = await _service.GetAsync(reminderId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateReminderAndReturnNewId()
    {
        var reminderId = Guid.NewGuid();
        var dto = new ReminderDto(reminderId, "Title", DateTime.UtcNow, false, null, null);

        _repo.Setup(repository => repository.AddAsync(It.IsAny<Reminder>(), AnyCancellationToken)).ReturnsAsync(reminderId);

        var result = await _service.AddAsync(dto);

        result.Should().Be(reminderId);
        _repo.Verify(repository => repository.AddAsync(It.Is<Reminder>(reminder => reminder.Title == dto.Title), AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task SetReminderAsFiredIfCreatingReminderWithFiredTrue()
    {
        var reminderId = Guid.NewGuid();
        var dto = new ReminderDto(reminderId, "Title", DateTime.UtcNow, true, null, null);

        _repo.Setup(repository => repository.AddAsync(It.IsAny<Reminder>(), AnyCancellationToken)).ReturnsAsync(reminderId);

        await _service.AddAsync(dto);

        _repo.Verify(repository => repository.AddAsync(It.Is<Reminder>(reminder => reminder.Fired), AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReturnAllRemindersIfListingReminders()
    {
        var reminders = new[]
        {
            new Reminder("Reminder 1", DateTime.UtcNow),
            new Reminder("Reminder 2", DateTime.UtcNow)
        };

        _repo.Setup(repository => repository.GetAllAsync(AnyCancellationToken)).ReturnsAsync(reminders.ToList());

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ReturnUpcomingRemindersIfRemindersExistBeforeDate()
    {
        var untilUtc = DateTime.UtcNow.AddDays(7);
        var reminders = new[]
        {
            new Reminder("Reminder 1", DateTime.UtcNow.AddDays(1)),
            new Reminder("Reminder 2", DateTime.UtcNow.AddDays(3))
        };

        _repo.Setup(repository => repository.UpcomingAsync(untilUtc, AnyCancellationToken)).ReturnsAsync(reminders.ToList());

        var result = await _service.UpcomingAsync(untilUtc);

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Reminder 1");
        result[1].Title.Should().Be("Reminder 2");
    }

    [Fact]
    public async Task MarkReminderAsFiredIfReminderExists()
    {
        var reminderId = Guid.NewGuid();

        _repo.Setup(repository => repository.MarkFiredAsync(reminderId, AnyCancellationToken)).Returns(Task.CompletedTask);

        await _service.MarkFiredAsync(reminderId);

        _repo.Verify(repository => repository.MarkFiredAsync(reminderId, AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task DeleteReminderIfReminderExists()
    {
        var reminderId = Guid.NewGuid();

        _repo.Setup(repository => repository.DeleteAsync(reminderId, AnyCancellationToken)).Returns(Task.CompletedTask);

        await _service.DeleteAsync(reminderId);

        _repo.Verify(repository => repository.DeleteAsync(reminderId, AnyCancellationToken), Times.Once);
    }
}
