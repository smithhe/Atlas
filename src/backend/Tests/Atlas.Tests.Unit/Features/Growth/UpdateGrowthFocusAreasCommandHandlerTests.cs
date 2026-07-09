using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.UpdateFocusAreas;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth;

public sealed class UpdateGrowthFocusAreasCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateGrowthFocusAreasCommandHandler _handler;

    public UpdateGrowthFocusAreasCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateGrowthFocusAreasCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenExists_UpdatesMarkdown()
    {
        var plan = new Domain.Entities.Growth { Id = Guid.NewGuid(), TeamMemberId = Guid.NewGuid(), FocusAreasMarkdown = "old" };
        _growth.Setup(g => g.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new UpdateGrowthFocusAreasCommand(plan.Id, "## New"), CancellationToken.None);

        ok.Should().BeTrue();
        plan.FocusAreasMarkdown.Should().Be("## New");
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _growth.Setup(g => g.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        bool ok = await _handler.Handle(new UpdateGrowthFocusAreasCommand(Guid.NewGuid(), "x"), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
