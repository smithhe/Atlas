using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.Goals.DeleteGrowthGoal;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.Goals;

public sealed class DeleteGrowthGoalCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteGrowthGoalCommandHandler _handler;

    public DeleteGrowthGoalCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteGrowthGoalCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenGoalExists_Removes()
    {
        var growthId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var plan = new Domain.Entities.Growth
        {
            Id = growthId,
            TeamMemberId = Guid.NewGuid(),
            Goals = [new GrowthGoal { Id = goalId, GrowthId = growthId, Title = "T", Description = "D", Status = GrowthGoalStatus.OnTrack }]
        };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new DeleteGrowthGoalCommand(growthId, goalId), CancellationToken.None);

        ok.Should().BeTrue();
        plan.Goals.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _growth.Setup(g => g.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        bool ok = await _handler.Handle(new DeleteGrowthGoalCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
