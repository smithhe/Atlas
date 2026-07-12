using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.Goals.UpdateGrowthGoal;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.Goals;

public sealed class UpdateGrowthGoalCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateGrowthGoalCommandHandler _handler;

    public UpdateGrowthGoalCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateGrowthGoalCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenGoalExists_Updates()
    {
        var growthId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new GrowthGoal { Id = goalId, GrowthId = growthId, Title = "Old", Description = "D", Status = GrowthGoalStatus.OnTrack };
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid(), Goals = [goal] };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new UpdateGrowthGoalCommand(
            growthId, goalId, " New ", " Desc ", GrowthGoalStatus.NeedsAttention, null, null, "Cat", Priority.High, 50, "Sum", "Criteria"), CancellationToken.None);

        ok.Should().BeTrue();
        goal.Title.Should().Be("New");
        goal.Status.Should().Be(GrowthGoalStatus.NeedsAttention);
        goal.ProgressPercent.Should().Be(50);
    }

    [Fact]
    public async Task Handle_WhenGoalMissing_ReturnsFalse()
    {
        var growthId = Guid.NewGuid();
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid() };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new UpdateGrowthGoalCommand(
            growthId, Guid.NewGuid(), "T", "D", GrowthGoalStatus.OnTrack, null, null, null, null, null, null, null), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
