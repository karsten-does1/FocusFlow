using FocusFlow.Core.Domain.Entities;
using FluentAssertions;

namespace FocusFlow.Tests.Domain.Entities;

public class EmailTests
{
    [Fact]
    public void CreateEmailWithAllProperties()
    {
        var email = new Email("from@test.com", "Subject", "Body", DateTime.UtcNow);

        email.From.Should().Be("from@test.com");
        email.Subject.Should().Be("Subject");
        email.BodyText.Should().Be("Body");
        email.PriorityScore.Should().Be(0);
        email.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void SetEmptyStringsWhenCreatingEmailWithNullValues()
    {
        var email = new Email(null!, null!, null!, DateTime.UtcNow);
        email.From.Should().BeEmpty();
        email.Subject.Should().BeEmpty();
        email.BodyText.Should().BeEmpty();
    }

    [Fact]
    public void SetPriorityWhenGivenValidScore()
    {
        var email = new Email("from@test.com", "Subject", "Body", DateTime.UtcNow);
        email.SetPriority(50);
        email.PriorityScore.Should().Be(50);
    }

    [Fact]
    public void ClampToZeroWhenSettingNegativePriority()
    {
        var email = new Email("from@test.com", "Subject", "Body", DateTime.UtcNow);
        email.SetPriority(-10);
        email.PriorityScore.Should().Be(0);
    }

    [Fact]
    public void ClampToHundredWhenSettingPriorityAboveHundred()
    {
        var email = new Email("from@test.com", "Subject", "Body", DateTime.UtcNow);
        email.SetPriority(150);
        email.PriorityScore.Should().Be(100);
    }

    [Fact]
    public void UpdateAllPropertiesWhenUpdatingEmail()
    {
        var email = new Email("old@test.com", "Old", "Old Body", DateTime.UtcNow);
        email.Update("new@test.com", "New", "New Body", DateTime.UtcNow.AddDays(1), 75);

        email.From.Should().Be("new@test.com");
        email.Subject.Should().Be("New");
        email.PriorityScore.Should().Be(75);
    }

    [Fact]
    public void SetEmptyStringsWhenUpdatingEmailWithNullValues()
    {
        var email = new Email("from@test.com", "Subject", "Body", DateTime.UtcNow);
        email.Update(null!, null!, null!, DateTime.UtcNow, 0);
        email.From.Should().BeEmpty();
        email.Subject.Should().BeEmpty();
        email.BodyText.Should().BeEmpty();
    }
}
