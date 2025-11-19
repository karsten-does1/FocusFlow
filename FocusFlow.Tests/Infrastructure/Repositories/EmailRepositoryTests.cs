using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Repositories;
using FluentAssertions;

namespace FocusFlow.Tests.Infrastructure.Repositories;

public class EmailRepositoryTests : RepositoryTestBase
{
    private readonly EmailRepository _repository;

    public EmailRepositoryTests()
    {
        _repository = new EmailRepository(DbContext);
    }

    [Fact]
    public async Task AddEmailReturnId()
    {
        var email = new Email("from@test.com", "Subject", "Body", DateTime.UtcNow);

        var result = await _repository.AddAsync(email);

        result.Should().Be(email.Id);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEmailIfExists()
    {
        var email = await AddToDatabaseAsync(new Email("from@test.com", "Subject", "Body", DateTime.UtcNow));

        var result = await _repository.GetAsync(email.Id);

        result.Should().NotBeNull();
        result!.From.Should().Be("from@test.com");
        result.Subject.Should().Be("Subject");
    }

    [Fact]
    public async Task ReturnNullIfDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _repository.GetAsync(nonExistentId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEmailForUpdateIfExists()
    {
        var email = await AddToDatabaseAsync(new Email("from@test.com", "Subject", "Body", DateTime.UtcNow));

        var result = await _repository.GetForUpdateAsync(email.Id);

        result.Should().NotBeNull();
        result!.From.Should().Be("from@test.com");
    }

    [Fact]
    public async Task GetLatestEmailsOrderedByReceivedDate()
    {
        var email1 = new Email("from1@test.com", "Subject 1", "Body 1", DateTime.UtcNow.AddDays(-2));
        var email2 = new Email("from2@test.com", "Subject 2", "Body 2", DateTime.UtcNow.AddDays(-1));
        await AddRangeToDatabaseAsync(new[] { email1, email2 });

        var result = await _repository.GetLatestAsync(null);

        result.Should().HaveCount(2);
        result[0].From.Should().Be("from2@test.com");
        result[1].From.Should().Be("from1@test.com");
    }

    [Fact]
    public async Task FilterEmailsBySearchQuery()
    {
        var email1 = new Email("john@test.com", "Subject", "Body", DateTime.UtcNow);
        var email2 = new Email("jane@test.com", "Subject", "Body", DateTime.UtcNow);
        await AddRangeToDatabaseAsync(new[] { email1, email2 });

        var result = await _repository.GetLatestAsync("john");

        result.Should().HaveCount(1);
        result[0].From.Should().Be("john@test.com");
    }

    [Fact]
    public async Task FilterEmailsBySubjectSearchQuery()
    {
        var email1 = new Email("from@test.com", "Important Subject", "Body", DateTime.UtcNow);
        var email2 = new Email("from@test.com", "Other Subject", "Body", DateTime.UtcNow);
        await AddRangeToDatabaseAsync(new[] { email1, email2 });

        var result = await _repository.GetLatestAsync("Important");

        result.Should().HaveCount(1);
        result[0].Subject.Should().Be("Important Subject");
    }

    [Fact]
    public async Task FilterEmailsByBodyTextSearchQuery()
    {
        var email1 = new Email("from@test.com", "Subject", "Important Body", DateTime.UtcNow);
        var email2 = new Email("from@test.com", "Subject", "Other Body", DateTime.UtcNow);
        await AddRangeToDatabaseAsync(new[] { email1, email2 });

        var result = await _repository.GetLatestAsync("Important");

        result.Should().HaveCount(1);
        result[0].BodyText.Should().Be("Important Body");
    }

    [Fact]
    public async Task LimitResultsTo100GettingLatestEmails()
    {
        var emails = Enumerable.Range(0, 150)
            .Select(i => new Email($"from{i}@test.com", "Subject", "Body", DateTime.UtcNow.AddMinutes(-i)))
            .ToList();
        await AddRangeToDatabaseAsync(emails);

        var result = await _repository.GetLatestAsync(null);

        result.Should().HaveCount(100);
    }

    [Fact]
    public async Task UpdateEmailIfEmailExists()
    {
        var email = await AddToDatabaseAsync(new Email("old@test.com", "Old", "Old Body", DateTime.UtcNow));
        email.Update("new@test.com", "New", "New Body", DateTime.UtcNow, 75);

        await _repository.UpdateAsync(email);

        var updatedEmail = await _repository.GetAsync(email.Id);
        updatedEmail.Should().NotBeNull();
        updatedEmail!.From.Should().Be("new@test.com");
        updatedEmail.Subject.Should().Be("New");
    }

    [Fact]
    public async Task DeleteEmailIfEmailExists()
    {
        var email = await AddToDatabaseAsync(new Email("from@test.com", "Subject", "Body", DateTime.UtcNow));

        await _repository.DeleteAsync(email.Id);

        var result = await _repository.GetAsync(email.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotThrowDeletingNonExistentEmail()
    {
        var nonExistentId = Guid.NewGuid();

        var action = async () => await _repository.DeleteAsync(nonExistentId);
        await action.Should().NotThrowAsync();
    }
}
