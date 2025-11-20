using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace FocusFlow.Tests.Infrastructure.Services;

public class EmailServiceTests : ServiceTestBase
{
    private readonly Mock<IEmailRepository> _repo = new();
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        _service = new EmailService(_repo.Object);
    }

    [Fact]
    public async Task ReturnLatestEmailsIfEmailsExist()
    {
        var emails = new[]
        {
            new Email("from1@test.com", "Subject 1", "Body 1", DateTime.UtcNow),
            new Email("from2@test.com", "Subject 2", "Body 2", DateTime.UtcNow)
        };

        _repo.Setup(repository => repository.GetLatestAsync(null, AnyCancellationToken)).ReturnsAsync(emails.ToList());

        var result = await _service.GetLatestAsync(null);

        result.Should().HaveCount(2);
        result[0].From.Should().Be("from1@test.com");
        result[1].From.Should().Be("from2@test.com");
    }

    [Fact]
    public async Task ReturnFilteredEmailsIfQueryIsProvided()
    {
        var emails = new[]
        {
            new Email("from@test.com", "Important Subject", "Body", DateTime.UtcNow)
        };

        _repo.Setup(repository => repository.GetLatestAsync("Important", AnyCancellationToken)).ReturnsAsync(emails.ToList());

        var result = await _service.GetLatestAsync("Important");

        result.Should().HaveCount(1);
        result[0].Subject.Should().Be("Important Subject");
    }

    [Fact]
    public async Task ReturnEmailIfEmailExists()
    {
        var emailId = Guid.NewGuid();
        var email = new Email("from@test.com", "Subject", "Body", DateTime.UtcNow);
        email.SetPriority(50);

        _repo.Setup(repository => repository.GetAsync(emailId, AnyCancellationToken)).ReturnsAsync(email);

        var result = await _service.GetAsync(emailId);

        result.Should().NotBeNull();
        result!.From.Should().Be(email.From);
        result.Subject.Should().Be(email.Subject);
    }

    [Fact]
    public async Task ReturnNullIfEmailDoesNotExist()
    {
        var emailId = Guid.NewGuid();

        _repo.Setup(repository => repository.GetAsync(emailId, AnyCancellationToken)).ReturnsAsync((Email?)null);

        var result = await _service.GetAsync(emailId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEmailAndReturnNewId()
    {
        var emailId = Guid.NewGuid();
        var dto = new EmailDto(emailId, "from@test.com", "Subject", "Body", DateTime.UtcNow, 50);

        _repo.Setup(repository => repository.AddAsync(It.IsAny<Email>(), AnyCancellationToken)).ReturnsAsync(emailId);

        var result = await _service.AddAsync(dto);

        result.Should().Be(emailId);
        _repo.Verify(repository => repository.AddAsync(It.Is<Email>(email => email.From == dto.From && email.Subject == dto.Subject), AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateEmailWithProviderAndAccountId()
    {
        var emailId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var dto = new EmailDto(emailId, "from@test.com", "Subject", "Body", DateTime.UtcNow, 50,
            Core.Domain.Enums.EmailProvider.Gmail, "external-msg-id", accountId);

        _repo.Setup(repository => repository.AddAsync(It.IsAny<Email>(), AnyCancellationToken)).ReturnsAsync(emailId);

        var result = await _service.AddAsync(dto);

        result.Should().Be(emailId);
        _repo.Verify(repository => repository.AddAsync(It.Is<Email>(email =>
            email.Provider == Core.Domain.Enums.EmailProvider.Gmail &&
            email.EmailAccountId == accountId &&
            email.ExternalMessageId == "external-msg-id"), AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateEmailIfEmailExists()
    {
        var emailId = Guid.NewGuid();
        var email = new Email("old@test.com", "Old", "Old Body", DateTime.UtcNow);
        email.SetPriority(10);
        var dto = new EmailDto(emailId, "new@test.com", "New", "New Body", DateTime.UtcNow, 75);

        _repo.Setup(repository => repository.GetForUpdateAsync(emailId, AnyCancellationToken)).ReturnsAsync(email);

        await _service.UpdateAsync(dto);

        email.From.Should().Be("new@test.com");
        email.Subject.Should().Be("New");
        email.BodyText.Should().Be("New Body");
        email.PriorityScore.Should().Be(75);

        _repo.Verify(repository => repository.UpdateAsync(email, AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateEmailWithProviderAndAccountId()
    {
        var emailId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var email = new Email("old@test.com", "Old", "Old Body", DateTime.UtcNow);
        var dto = new EmailDto(emailId, "new@test.com", "New", "New Body", DateTime.UtcNow, 75,
            Core.Domain.Enums.EmailProvider.Outlook, "new-external-id", accountId);

        _repo.Setup(repository => repository.GetForUpdateAsync(emailId, AnyCancellationToken)).ReturnsAsync(email);

        await _service.UpdateAsync(dto);

        email.Provider.Should().Be(Core.Domain.Enums.EmailProvider.Outlook);
        email.EmailAccountId.Should().Be(accountId);
        email.ExternalMessageId.Should().Be("new-external-id");

        _repo.Verify(repository => repository.UpdateAsync(email, AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task ShouldThrowExceptionWhenUpdatingNonExistentEmail()
    {
        var emailId = Guid.NewGuid();
        var dto = new EmailDto(emailId, "from@test.com", "Subject", "Body", DateTime.UtcNow, 0);

        _repo.Setup(repository => repository.GetForUpdateAsync(emailId, AnyCancellationToken)).ReturnsAsync((Email?)null);

        var action = async () => await _service.UpdateAsync(dto);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"Email with id {emailId} not found.");
    }

    [Fact]
    public async Task DeleteEmailIfEmailExists()
    {
        var emailId = Guid.NewGuid();

        await _service.DeleteAsync(emailId);

        _repo.Verify(repository => repository.DeleteAsync(emailId, AnyCancellationToken), Times.Once);
    }
}
