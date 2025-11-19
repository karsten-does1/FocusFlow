using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Repositories;
using FluentAssertions;

namespace FocusFlow.Tests.Infrastructure.Repositories;

public class SummaryRepositoryTests : RepositoryTestBase
{
    private readonly SummaryRepository _repository;

    public SummaryRepositoryTests()
    {
        _repository = new SummaryRepository(DbContext);
    }

    [Fact]
    public async Task AddSummaryReturnId()
    {
        var emailId = Guid.NewGuid();
        var summary = new Summary(emailId, "Summary text");

        var result = await _repository.AddAsync(summary);

        result.Should().Be(summary.Id);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSummaryIfSummaryExists()
    {
        var emailId = Guid.NewGuid();
        var summary = await AddToDatabaseAsync(new Summary(emailId, "Summary text"));

        var result = await _repository.GetAsync(summary.Id);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Summary text");
        result.EmailId.Should().Be(emailId);
    }

    [Fact]
    public async Task ReturnNullIfSummaryDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _repository.GetAsync(nonExistentId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSummaryForUpdateIfSummaryExists()
    {
        var emailId = Guid.NewGuid();
        var summary = await AddToDatabaseAsync(new Summary(emailId, "Summary text"));

        var result = await _repository.GetForUpdateAsync(summary.Id);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Summary text");
    }

    [Fact]
    public async Task GetSummaryByEmailIdIfSummaryExists()
    {
        var emailId = Guid.NewGuid();
        var summary = await AddToDatabaseAsync(new Summary(emailId, "Summary text"));

        var result = await _repository.GetByEmailIdAsync(emailId);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Summary text");
        result.EmailId.Should().Be(emailId);
    }

    [Fact]
    public async Task ReturnNullIfNoSummaryExistsForEmail()
    {
        var emailId = Guid.NewGuid();

        var result = await _repository.GetByEmailIdAsync(emailId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReturnMostRecentSummaryIfMultipleSummariesExistForEmail()
    {
        var emailId = Guid.NewGuid();
        var summary1 = new Summary(emailId, "Old Summary");
        await AddToDatabaseAsync(summary1);
        await Task.Delay(10);
        var summary2 = new Summary(emailId, "New Summary");
        await AddToDatabaseAsync(summary2);

        var result = await _repository.GetByEmailIdAsync(emailId);

        result.Should().NotBeNull();
        result!.Text.Should().Be("New Summary");
    }

    [Fact]
    public async Task GetSummaryForUpdateByEmailIdIfSummaryExists()
    {
        var emailId = Guid.NewGuid();
        var summary = await AddToDatabaseAsync(new Summary(emailId, "Summary text"));

        var result = await _repository.GetForUpdateByEmailIdAsync(emailId);

        result.Should().NotBeNull();
        result!.Text.Should().Be("Summary text");
    }

    [Fact]
    public async Task UpdateSummaryIfSummaryExists()
    {
        var emailId = Guid.NewGuid();
        var summary = await AddToDatabaseAsync(new Summary(emailId, "Old Text"));
        summary.UpdateText("New Text");

        await _repository.UpdateAsync(summary);

        var updatedSummary = await _repository.GetAsync(summary.Id);
        updatedSummary.Should().NotBeNull();
        updatedSummary!.Text.Should().Be("New Text");
    }

    [Fact]
    public async Task DeleteSummaryIfSummaryExists()
    {
        var emailId = Guid.NewGuid();
        var summary = await AddToDatabaseAsync(new Summary(emailId, "Summary text"));

        await _repository.DeleteAsync(summary.Id);

        var result = await _repository.GetAsync(summary.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotThrowWhenDeletingNonExistentSummary()
    {
        var nonExistentId = Guid.NewGuid();

        var action = async () => await _repository.DeleteAsync(nonExistentId);
        await action.Should().NotThrowAsync();
    }
}
