using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.Goals.CheckIns.DeleteGrowthGoalCheckIn;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.Goals.CheckIns;

public sealed class DeleteGrowthGoalCheckInCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteGrowthGoalCheckInCommandHandler _handler;

    public DeleteGrowthGoalCheckInCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteGrowthGoalCheckInCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenCheckInExists_Removes()
    {
        Guid growthId = Guid.NewGuid();
        Guid goalId = Guid.NewGuid();
        Guid checkInId = Guid.NewGuid();
        var goal = new GrowthGoal
        {
            Id = goalId,
            GrowthId = growthId,
            Title = "G",
            Description = "D",
            Status = GrowthGoalStatus.OnTrack,
            CheckIns = [new GrowthGoalCheckIn { Id = checkInId, GrowthGoalId = goalId, Date = DateOnly.FromDateTime(DateTime.UtcNow), Signal = GrowthGoalCheckInSignal.Positive, Note = "n" }]
        };
        var plan = new Domain.Entities.Growth { Id = growthId, TeamMemberId = Guid.NewGuid(), Goals = [goal] };
        _growth.Setup(g => g.GetByIdWithDetailsAsync(growthId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        bool ok = await _handler.Handle(new DeleteGrowthGoalCheckInCommand(growthId, goalId, checkInId), CancellationToken.None);

        ok.Should().BeTrue();
        goal.CheckIns.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _growth.Setup(g => g.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        bool ok = await _handler.Handle(new DeleteGrowthGoalCheckInCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
