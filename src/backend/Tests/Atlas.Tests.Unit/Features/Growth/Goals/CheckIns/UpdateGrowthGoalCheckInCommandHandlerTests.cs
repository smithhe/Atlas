using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.Goals.CheckIns.UpdateGrowthGoalCheckIn;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.Goals.CheckIns;

public sealed class UpdateGrowthGoalCheckInCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateGrowthGoalCheckInCommandHandler _handler;

    public UpdateGrowthGoalCheckInCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateGrowthGoalCheckInCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenCheckInExists_Updates()
    {
        var growthId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var checkInId = Guid.NewGuid();
        var checkIn = new GrowthGoalCheckIn { Id = checkInId, GrowthGoalId = goalId, Date = DateOnly.FromDateTime(DateTime.UtcNow), Signal = GrowthGoalCheckInSignal.Mixed, Note = "Old" };
        var goal = new GrowthGoal { Id = goalId, GrowthId = growthId, Title = "G", Description = "D", Status = GrowthGoalStatus.OnTrack, CheckIns = [checkIn] };
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid(), Goals = [goal] };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new UpdateGrowthGoalCheckInCommand(
            growthId, goalId, checkInId, checkIn.Date, GrowthGoalCheckInSignal.Concern, " New "), CancellationToken.None);

        ok.Should().BeTrue();
        checkIn.Note.Should().Be("New");
        checkIn.Signal.Should().Be(GrowthGoalCheckInSignal.Concern);
    }

    [Fact]
    public async Task Handle_WhenCheckInMissing_ReturnsFalse()
    {
        var growthId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var goal = new GrowthGoal { Id = goalId, GrowthId = growthId, Title = "G", Description = "D", Status = GrowthGoalStatus.OnTrack };
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid(), Goals = [goal] };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new UpdateGrowthGoalCheckInCommand(
            growthId, goalId, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), GrowthGoalCheckInSignal.Positive, "n"), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
