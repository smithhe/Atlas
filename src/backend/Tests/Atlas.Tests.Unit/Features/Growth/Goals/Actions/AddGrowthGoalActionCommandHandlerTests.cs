using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.Goals.Actions.AddGrowthGoalAction;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.Goals.Actions;

public sealed class AddGrowthGoalActionCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly AddGrowthGoalActionCommandHandler _handler;

    public AddGrowthGoalActionCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new AddGrowthGoalActionCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenGoalExists_AddsAction()
    {
        var growthId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid() };
        var goal = new GrowthGoal { Id = goalId, GrowthId = growthId, Title = "G", Description = "D", Status = GrowthGoalStatus.OnTrack };
        _growth.Setup(g => g.GetByIdAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _growth.Setup(g => g.GetGoalByIdAsync(goalId, It.IsAny<CancellationToken>())).ReturnsAsync(goal);
        GrowthGoalAction? captured = null;
        _growth.Setup(g => g.AddGoalActionAsync(It.IsAny<GrowthGoalAction>(), It.IsAny<CancellationToken>()))
            .Callback<GrowthGoalAction, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);

        Guid id = await _handler.Handle(new AddGrowthGoalActionCommand(
            growthId, goalId, " Act ", GrowthGoalActionState.Planned, null, Priority.Low, "n", "e"), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured!.Title.Should().Be("Act");
    }

    [Fact]
    public async Task Handle_WhenGoalBelongsToOtherPlan_ReturnsEmptyGuid()
    {
        var growthId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid() };
        var goal = new GrowthGoal { Id = goalId, GrowthId = Guid.NewGuid(), Title = "G", Description = "D", Status = GrowthGoalStatus.OnTrack };
        _growth.Setup(g => g.GetByIdAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _growth.Setup(g => g.GetGoalByIdAsync(goalId, It.IsAny<CancellationToken>())).ReturnsAsync(goal);

        Guid id = await _handler.Handle(new AddGrowthGoalActionCommand(
            growthId, goalId, "A", GrowthGoalActionState.Planned, null, null, null, null), CancellationToken.None);

        id.Should().Be(Guid.Empty);
    }
}
