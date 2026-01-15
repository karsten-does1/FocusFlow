using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Mappers;
using FluentAssertions;

namespace FocusFlow.Tests.Infrastructure.Mappers;

public class ReminderMapperTests
{
    [Fact]
    public void MapAllPropertiesConvertingToDto()
    {
        var fireAtUtc = DateTime.UtcNow;
        var relatedTaskId = Guid.NewGuid();
        var relatedEmailId = Guid.NewGuid();
        var reminder = new Reminder("Title", fireAtUtc, relatedTaskId, relatedEmailId);
        reminder.MarkFired();

        var dto = ReminderMapper.ToDto(reminder);

        dto.Id.Should().Be(reminder.Id);
        dto.Title.Should().Be("Title");
        dto.FireAtUtc.Should().Be(fireAtUtc);
        dto.Fired.Should().BeTrue();
        dto.RelatedTaskId.Should().Be(relatedTaskId);
        dto.RelatedEmailId.Should().Be(relatedEmailId);
    }
}
