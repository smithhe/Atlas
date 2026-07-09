using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.AzureDevOps.Connection;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class GetAzureConnectionQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSingleton()
    {
        var connections = new Mock<IAzureConnectionRepository>();
        var connection = new AzureConnection { Id = Guid.NewGuid(), Organization = "org", Project = "proj" };
        connections.Setup(c => c.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);

        var handler = new GetAzureConnectionQueryHandler(connections.Object);
        (await handler.Handle(new GetAzureConnectionQuery(), CancellationToken.None)).Should().BeSameAs(connection);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsNull()
    {
        var connections = new Mock<IAzureConnectionRepository>();
        connections.Setup(c => c.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AzureConnection?)null);

        var handler = new GetAzureConnectionQueryHandler(connections.Object);
        (await handler.Handle(new GetAzureConnectionQuery(), CancellationToken.None)).Should().BeNull();
    }
}
