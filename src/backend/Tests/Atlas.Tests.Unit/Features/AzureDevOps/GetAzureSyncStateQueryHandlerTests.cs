using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.AzureDevOps.SyncState;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class GetAzureSyncStateQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoConnection_ReturnsNull()
    {
        var connections = new Mock<IAzureConnectionRepository>();
        var syncStates = new Mock<IAzureSyncStateRepository>();
        connections.Setup(c => c.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AzureConnection?)null);

        var handler = new GetAzureSyncStateQueryHandler(connections.Object, syncStates.Object);
        (await handler.Handle(new GetAzureSyncStateQuery(), CancellationToken.None)).Should().BeNull();
        syncStates.Verify(s => s.GetByConnectionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenConnectionExists_LoadsSyncState()
    {
        var connections = new Mock<IAzureConnectionRepository>();
        var syncStates = new Mock<IAzureSyncStateRepository>();
        var connection = new AzureConnection { Id = Guid.NewGuid() };
        var state = new AzureSyncState { Id = Guid.NewGuid(), AzureConnectionId = connection.Id };
        connections.Setup(c => c.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        syncStates.Setup(s => s.GetByConnectionIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(state);

        var handler = new GetAzureSyncStateQueryHandler(connections.Object, syncStates.Object);
        (await handler.Handle(new GetAzureSyncStateQuery(), CancellationToken.None)).Should().BeSameAs(state);
    }
}
