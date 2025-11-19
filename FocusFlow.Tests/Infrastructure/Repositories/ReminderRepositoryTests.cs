using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Repositories;
using FluentAssertions;

namespace FocusFlow.Tests.Infrastructure.Repositories;

public class ReminderRepositoryTests : RepositoryTestBase
{
    private readonly ReminderRepository _repository;

    public ReminderRepositoryTests()
    {
        _repository = new ReminderRepository(DbContext);
    }

    [Fact]
    public async Task AddReminderReturnId()
    {
        var reminder = new Reminder("Test Reminder", DateTime.UtcNow.AddDays(1));

        var result = await _repository.AddAsync(reminder);

        result.Should().Be(reminder.Id);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetReminderIfReminderExists()
    {
        var reminder = await AddToDatabaseAsync(new Reminder("Test Reminder", DateTime.UtcNow.AddDays(1)));

        var result = await _repository.GetAsync(reminder.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Reminder");
    }

    [Fact]
    public async Task ShouldReturnNullIfReminderDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _repository.GetAsync(nonExistentId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReminderForUpdateIfReminderExists()
    {
        var reminder = await AddToDatabaseAsync(new Reminder("Test Reminder", DateTime.UtcNow.AddDays(1)));

        var result = await _repository.GetForUpdateAsync(reminder.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Reminder");
    }

    [Fact]
    public async Task GetAllRemindersOrderedByFireAtUtc()
    {
        var reminder1 = new Reminder("Reminder 1", DateTime.UtcNow.AddDays(3));
        var reminder2 = new Reminder("Reminder 2", DateTime.UtcNow.AddDays(1));
        await AddRangeToDatabaseAsync(new[] { reminder1, reminder2 });

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Reminder 2");
        result[1].Title.Should().Be("Reminder 1");
    }

    [Fact]
    public async Task GetUpcomingRemindersWhenNotFiredAndBeforeDate()
    {
        var reminder1 = new Reminder("Reminder 1", DateTime.UtcNow.AddDays(1));
        var reminder2 = new Reminder("Reminder 2", DateTime.UtcNow.AddDays(3));
        var reminder3 = new Reminder("Reminder 3", DateTime.UtcNow.AddDays(5));
        reminder3.MarkFired();
        await AddRangeToDatabaseAsync(new[] { reminder1, reminder2, reminder3 });

        var result = await _repository.UpcomingAsync(DateTime.UtcNow.AddDays(4));

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Reminder 1");
        result[1].Title.Should().Be("Reminder 2");
    }

    [Fact]
    public async Task NoReturnFiredRemindersWhenGettingUpcoming()
    {
        var reminder1 = new Reminder("Reminder 1", DateTime.UtcNow.AddDays(1));
        var reminder2 = new Reminder("Reminder 2", DateTime.UtcNow.AddDays(2));
        reminder2.MarkFired();
        await AddRangeToDatabaseAsync(new[] { reminder1, reminder2 });

        var result = await _repository.UpcomingAsync(DateTime.UtcNow.AddDays(3));

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Reminder 1");
    }

    [Fact]
    public async Task NoReturnFutureRemindersWhenGettingUpcoming()
    {
        var reminder1 = new Reminder("Reminder 1", DateTime.UtcNow.AddDays(1));
        var reminder2 = new Reminder("Reminder 2", DateTime.UtcNow.AddDays(5));
        await AddRangeToDatabaseAsync(new[] { reminder1, reminder2 });

        var result = await _repository.UpcomingAsync(DateTime.UtcNow.AddDays(3));

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Reminder 1");
    }

    [Fact]
    public async Task ShouldMarkReminderAsFiredIfReminderExists()
    {
        var reminder = await AddToDatabaseAsync(new Reminder("Test Reminder", DateTime.UtcNow.AddDays(1)));

        await _repository.MarkFiredAsync(reminder.Id);

        var updatedReminder = await _repository.GetAsync(reminder.Id);
        updatedReminder.Should().NotBeNull();
        updatedReminder!.Fired.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotThrowMarkingFiredOnNonExistentReminder()
    {
        var nonExistentId = Guid.NewGuid();

        var action = async () => await _repository.MarkFiredAsync(nonExistentId);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteReminderIfReminderExists()
    {
        var reminder = await AddToDatabaseAsync(new Reminder("Test Reminder", DateTime.UtcNow.AddDays(1)));

        await _repository.DeleteAsync(reminder.Id);

        var result = await _repository.GetAsync(reminder.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotThrowDeletingNonExistentReminder()
    {
        var nonExistentId = Guid.NewGuid();

        var action = async () => await _repository.DeleteAsync(nonExistentId);
        await action.Should().NotThrowAsync();
    }
}
