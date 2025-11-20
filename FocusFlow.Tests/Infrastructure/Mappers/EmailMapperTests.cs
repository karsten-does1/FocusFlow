using FocusFlow.Core.Domain.Entities;
using FocusFlow.Infrastructure.Mappers;
using FluentAssertions;

namespace FocusFlow.Tests.Infrastructure.Mappers;

public class EmailMapperTests
{
    [Fact]
    public void MapAllPropertiesConvertingToDto()
    {
        var receivedUtc = DateTime.UtcNow;
        var email = new Email("from@test.com", "Subject", "Body", receivedUtc);
        email.SetPriority(50);

        var dto = EmailMapper.ToDto(email);

        dto.Id.Should().Be(email.Id);
        dto.From.Should().Be("from@test.com");
        dto.Subject.Should().Be("Subject");
        dto.BodyText.Should().Be("Body");
        dto.ReceivedUtc.Should().Be(receivedUtc);
        dto.PriorityScore.Should().Be(50);
        dto.Provider.Should().Be(Core.Domain.Enums.EmailProvider.Unknown);
        dto.ExternalMessageId.Should().BeNull();
        dto.EmailAccountId.Should().BeNull();
    }

    [Fact]
    public void MapEmailWithProviderAndAccountId()
    {
        var receivedUtc = DateTime.UtcNow;
        var accountId = Guid.NewGuid();
        var email = new Email("from@test.com", "Subject", "Body", receivedUtc,
            Core.Domain.Enums.EmailProvider.Gmail, "external-msg-id", accountId);
        email.SetPriority(50);

        var dto = EmailMapper.ToDto(email);

        dto.Provider.Should().Be(Core.Domain.Enums.EmailProvider.Gmail);
        dto.ExternalMessageId.Should().Be("external-msg-id");
        dto.EmailAccountId.Should().Be(accountId);
    }
}
