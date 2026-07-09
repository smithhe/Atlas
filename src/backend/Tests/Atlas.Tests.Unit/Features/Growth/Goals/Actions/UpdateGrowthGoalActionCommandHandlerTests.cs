using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.Goals.Actions.UpdateGrowthGoalAction;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.Goals.Actions;

public sealed class UpdateGrowthGoalActionCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateGrowthGoalActionCommandHandler _handler;

    public UpdateGrowthGoalActionCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateGrowthGoalActionCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenActionExists_Updates()
    {
        Guid growthId = Guid.NewGuid();
        Guid goalId = Guid.NewGuid();
        Guid actionId = Guid.NewGuid();
        var action = new GrowthGoalAction { Id = actionId, GrowthGoalId = goalId, Title = "Old", State = GrowthGoalActionState.Planned };
        var goal = new GrowthGoal { Id = goalId, GrowthId = growthId, Title = "G", Description = "D", Status = GrowthGoalStatus.OnTrack, Actions = [action] };
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid(), Goals = [goal] };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new UpdateGrowthGoalActionCommand(
            growthId, goalId, actionId, " New ", GrowthGoalActionState.Complete, null, Priority.High, "n", "e"), CancellationToken.None);

        ok.Should().BeTrue();
        action.Title.Should().Be("New");
        action.State.Should().Be(GrowthGoalActionState.Complete);
    }

    [Fact]
    public async Task Handle_WhenActionMissing_ReturnsFalse()
    {
        Guid growthId = Guid.NewGuid();
        Guid goalId = Guid.NewGuid();
        var goal = new GrowthGoal { Id = goalId, GrowthId = growthId, Title = "G", Description = "D", Status = GrowthGoalStatus.OnTrack };
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid(), Goals = [goal] };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new UpdateGrowthGoalActionCommand(
            growthId, goalId, Guid.NewGuid(), "T", GrowthGoalActionState.Planned, null, null, null, null), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
