using Moq;

namespace FocusFlow.Tests.Infrastructure.Services;

public abstract class ServiceTestBase
{
    protected static CancellationToken AnyCancellationToken => It.IsAny<CancellationToken>();
}

