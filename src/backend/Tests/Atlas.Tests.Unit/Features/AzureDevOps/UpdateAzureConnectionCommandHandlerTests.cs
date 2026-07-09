using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.AzureDevOps.Connection;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class UpdateAzureConnectionCommandHandlerTests
{
    private readonly Mock<IAzureConnectionRepository> _connections = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateAzureConnectionCommandHandler _handler;

    public UpdateAzureConnectionCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateAzureConnectionCommandHandler(_connections.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenExisting_UpdatesFields()
    {
        var existing = new AzureConnection { Id = Guid.NewGuid() };
        _connections.Setup(c => c.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        bool ok = await _handler.Handle(new UpdateAzureConnectionCommand(
            " Org ", " Proj ", " Area ", " Team ", true, "pid", "tid"), CancellationToken.None);

        ok.Should().BeTrue();
        existing.Organization.Should().Be("Org");
        existing.Project.Should().Be("Proj");
        existing.AreaPath.Should().Be("Area");
        existing.TeamName.Should().Be("Team");
        existing.ProjectId.Should().Be("pid");
        existing.TeamId.Should().Be("tid");
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMissing_CreatesThenUpdates()
    {
        _connections.Setup(c => c.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AzureConnection?)null);
        AzureConnection? captured = null;
        _connections.Setup(c => c.AddAsync(It.IsAny<AzureConnection>(), It.IsAny<CancellationToken>()))
            .Callback<AzureConnection, CancellationToken>((c, _) => captured = c)
            .Returns(Task.CompletedTask);

        bool ok = await _handler.Handle(new UpdateAzureConnectionCommand(
            "org", "proj", "area", "  ", false, "pid", "tid"), CancellationToken.None);

        ok.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.TeamName.Should().BeNull();
        captured.IsEnabled.Should().BeFalse();
    }
}
