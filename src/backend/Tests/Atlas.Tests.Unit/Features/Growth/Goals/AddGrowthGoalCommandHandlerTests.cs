using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.Goals.AddGrowthGoal;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth.Goals;

public sealed class AddGrowthGoalCommandHandlerTests
{
    private readonly Mock<IGrowthRepository> _growth = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly AddGrowthGoalCommandHandler _handler;

    public AddGrowthGoalCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new AddGrowthGoalCommandHandler(_growth.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenPlanExists_AddsGoal()
    {
        var plan = new Domain.Entities.Growth { Id = Guid.NewGuid(), TeamMemberId = Guid.NewGuid() };
        _growth.Setup(g => g.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        GrowthGoal? captured = null;
        _growth.Setup(g => g.AddGoalAsync(It.IsAny<GrowthGoal>(), It.IsAny<CancellationToken>()))
            .Callback<GrowthGoal, CancellationToken>((goal, _) => captured = goal)
            .Returns(Task.CompletedTask);

        Guid id = await _handler.Handle(new AddGrowthGoalCommand(
            plan.Id, "  Title  ", " Desc ", GrowthGoalStatus.OnTrack, null, null, " Eng ", Priority.Medium), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured!.Title.Should().Be("Title");
        captured.Category.Should().Be("Eng");
    }

    [Fact]
    public async Task Handle_WhenPlanMissing_ReturnsEmptyGuid()
    {
        _growth.Setup(g => g.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        Guid id = await _handler.Handle(new AddGrowthGoalCommand(
            Guid.NewGuid(), "T", "D", GrowthGoalStatus.OnTrack, null, null, null, null), CancellationToken.None);

        id.Should().Be(Guid.Empty);
    }
}
