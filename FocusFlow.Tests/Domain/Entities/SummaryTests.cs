using FocusFlow.Core.Domain.Entities;
using FluentAssertions;

namespace FocusFlow.Tests.Domain.Entities;

public class SummaryTests
{
    [Fact]
    public void CreateSummaryWithAllProperties()
    {
        var emailId = Guid.NewGuid();
        var summary = new Summary(emailId, "Summary text");

        summary.EmailId.Should().Be(emailId);
        summary.Text.Should().Be("Summary text");
        summary.Id.Should().NotBeEmpty();
        summary.CreatedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetEmptyStringWhenCreatingSummaryWithNullText()
    {
        var emailId = Guid.NewGuid();
        var summary = new Summary(emailId, null!);

        summary.Text.Should().BeEmpty();
        summary.EmailId.Should().Be(emailId);
    }

    [Fact]
    public void UpdateTextAndCreatedUtcWhenUpdatingSummaryText()
    {
        var emailId = Guid.NewGuid();
        var summary = new Summary(emailId, "Old text");
        var originalCreatedUtc = summary.CreatedUtc;


        Thread.Sleep(10);
        summary.UpdateText("New text");

        summary.Text.Should().Be("New text");
        summary.CreatedUtc.Should().BeAfter(originalCreatedUtc);
    }

    [Fact]
    public void SetEmptyStringWhenUpdatingSummaryWithNullText()
    {
        var emailId = Guid.NewGuid();
        var summary = new Summary(emailId, "Original text");

        summary.UpdateText(null!);

        summary.Text.Should().BeEmpty();
    }
}

