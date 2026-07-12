using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.Goals.Actions.DeleteGrowthGoalAction;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.Goals.Actions;

public sealed class DeleteGrowthGoalActionCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteGrowthGoalActionCommandHandler _handler;

    public DeleteGrowthGoalActionCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteGrowthGoalActionCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenActionExists_Removes()
    {
        var growthId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        var goal = new GrowthGoal
        {
            Id = goalId,
            GrowthId = growthId,
            Title = "G",
            Description = "D",
            Status = GrowthGoalStatus.OnTrack,
            Actions = [new GrowthGoalAction { Id = actionId, GrowthGoalId = goalId, Title = "A", State = GrowthGoalActionState.Planned }]
        };
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid(), Goals = [goal] };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new DeleteGrowthGoalActionCommand(growthId, goalId, actionId), CancellationToken.None);

        ok.Should().BeTrue();
        goal.Actions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _growth.Setup(g => g.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        bool ok = await _handler.Handle(new DeleteGrowthGoalActionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
