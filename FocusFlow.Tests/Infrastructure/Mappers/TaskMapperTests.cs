using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Mappers;
using FluentAssertions;

namespace FocusFlow.Tests.Infrastructure.Mappers;

public class TaskMapperTests
{
    [Fact]
    public void MapAllPropertiesConvertingToDto()
    {
        var dueUtc = DateTime.UtcNow;
        var relatedEmailId = Guid.NewGuid();
        var task = new FocusTask("Title", "Notes", dueUtc, relatedEmailId);
        task.Complete();

        var dto = TaskMapper.ToDto(task);

        dto.Id.Should().Be(task.Id);
        dto.Title.Should().Be("Title");
        dto.Notes.Should().Be("Notes");
        dto.DueUtc.Should().Be(dueUtc);
        dto.IsDone.Should().BeTrue();
        dto.RelatedEmailId.Should().Be(relatedEmailId);
    }
}
