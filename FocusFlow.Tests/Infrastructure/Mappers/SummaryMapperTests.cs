using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Mappers;
using FluentAssertions;

namespace FocusFlow.Tests.Infrastructure.Mappers;

public class SummaryMapperTests
{
    [Fact]
    public void MapAllPropertiesConvertingToDto()
    {
        var emailId = Guid.NewGuid();
        var createdUtc = DateTime.UtcNow;
        var summary = new Summary(emailId, "Summary text");

        var dto = SummaryMapper.ToDto(summary);

        dto.Id.Should().Be(summary.Id);
        dto.EmailId.Should().Be(emailId);
        dto.Text.Should().Be("Summary text");
        dto.CreatedUtc.Should().BeCloseTo(createdUtc, TimeSpan.FromSeconds(1));
    }
}
