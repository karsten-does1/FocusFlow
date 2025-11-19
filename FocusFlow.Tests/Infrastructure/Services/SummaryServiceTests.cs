using FocusFlow.Core.Application.Contracts.DTOs;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace FocusFlow.Tests.Infrastructure.Services;

public class SummaryServiceTests : ServiceTestBase
{
    private readonly Mock<ISummaryRepository> _repo = new();
    private readonly SummaryService _service;

    public SummaryServiceTests()
    {
        _service = new SummaryService(_repo.Object);
    }

    [Fact]
    public async Task ReturnSummaryIfSummaryExists()
    {
        var summaryId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var summary = new Summary(emailId, "Text");

        _repo.Setup(repository => repository.GetAsync(summaryId, AnyCancellationToken)).ReturnsAsync(summary);

        var result = await _service.GetAsync(summaryId);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Text");
    }

    [Fact]
    public async Task ReturnNullIfSummaryDoesNotExist()
    {
        var summaryId = Guid.NewGuid();

        _repo.Setup(repository => repository.GetAsync(summaryId, AnyCancellationToken)).ReturnsAsync((Summary?)null);

        var result = await _service.GetAsync(summaryId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReturnSummaryIfSummaryExistsForEmail()
    {
        var emailId = Guid.NewGuid();
        var summary = new Summary(emailId, "Text");

        _repo.Setup(repository => repository.GetByEmailIdAsync(emailId, AnyCancellationToken)).ReturnsAsync(summary);

        var result = await _service.GetByEmailIdAsync(emailId);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Text");
    }

    [Fact]
    public async Task ReturnNullIfNoSummaryExistsForEmail()
    {
        var emailId = Guid.NewGuid();

        _repo.Setup(repository => repository.GetByEmailIdAsync(emailId, AnyCancellationToken)).ReturnsAsync((Summary?)null);

        var result = await _service.GetByEmailIdAsync(emailId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateSummaryAndReturnNewId()
    {
        var summaryId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var dto = new SummaryDto(summaryId, emailId, "Text", DateTime.UtcNow);

        _repo.Setup(repository => repository.AddAsync(It.IsAny<Summary>(), AnyCancellationToken)).ReturnsAsync(summaryId);

        var result = await _service.AddAsync(dto);

        result.Should().Be(summaryId);
        _repo.Verify(repository => repository.AddAsync(It.Is<Summary>(summary => summary.Text == dto.Text && summary.EmailId == dto.EmailId), AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateSummaryIfSummaryExists()
    {
        var summaryId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        var summary = new Summary(emailId, "Old Text");
        var dto = new SummaryDto(summaryId, emailId, "New Text", DateTime.UtcNow);

        _repo.Setup(repository => repository.GetForUpdateAsync(summaryId, AnyCancellationToken)).ReturnsAsync(summary);

        await _service.UpdateAsync(dto);

        summary.Text.Should().Be("New Text");
        _repo.Verify(repository => repository.UpdateAsync(summary, AnyCancellationToken), Times.Once);
    }

    [Fact]
    public async Task ShouldThrowExceptionIfUpdatingNonExistentSummary()
    {
        var summaryId = Guid.NewGuid();
        var dto = new SummaryDto(summaryId, Guid.NewGuid(), "Text", DateTime.UtcNow);

        _repo.Setup(repository => repository.GetForUpdateAsync(summaryId, AnyCancellationToken)).ReturnsAsync((Summary?)null);

        var action = async () => await _service.UpdateAsync(dto);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"Summary with id {summaryId} not found.");
    }

    [Fact]
    public async Task DeleteSummaryIfSummaryExists()
    {
        var summaryId = Guid.NewGuid();

        await _service.DeleteAsync(summaryId);

        _repo.Verify(repository => repository.DeleteAsync(summaryId, AnyCancellationToken), Times.Once);
    }
}
