using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.Goals.CheckIns.AddGrowthGoalCheckIn;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.Goals.CheckIns;

public sealed class AddGrowthGoalCheckInCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly AddGrowthGoalCheckInCommandHandler _handler;

    public AddGrowthGoalCheckInCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new AddGrowthGoalCheckInCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenGoalExists_AddsCheckIn()
    {
        var growthId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid() };
        var goal = new GrowthGoal { Id = goalId, GrowthId = growthId, Title = "G", Description = "D", Status = GrowthGoalStatus.OnTrack };
        _growth.Setup(g => g.GetByIdAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        _growth.Setup(g => g.GetGoalByIdAsync(goalId, It.IsAny<CancellationToken>())).ReturnsAsync(goal);
        GrowthGoalCheckIn? captured = null;
        _growth.Setup(g => g.AddGoalCheckInAsync(It.IsAny<GrowthGoalCheckIn>(), It.IsAny<CancellationToken>()))
            .Callback<GrowthGoalCheckIn, CancellationToken>((c, _) => captured = c)
            .Returns(Task.CompletedTask);

        Guid id = await _handler.Handle(new AddGrowthGoalCheckInCommand(
            growthId, goalId, DateOnly.FromDateTime(DateTime.UtcNow), GrowthGoalCheckInSignal.Positive, "  Good  "), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured!.Note.Should().Be("Good");
    }

    [Fact]
    public async Task Handle_WhenPlanMissing_ReturnsEmptyGuid()
    {
        _growth.Setup(g => g.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        Guid id = await _handler.Handle(new AddGrowthGoalCheckInCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), GrowthGoalCheckInSignal.Mixed, "n"), CancellationToken.None);

        id.Should().Be(Guid.Empty);
    }
}
