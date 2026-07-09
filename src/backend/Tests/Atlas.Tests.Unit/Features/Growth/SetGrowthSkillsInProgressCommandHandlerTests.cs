using Atlas.Application.Features.Growth.Skills.SetGrowthSkillsInProgress;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.Growth;

public sealed class SetGrowthSkillsInProgressCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenGrowthMissing_ReturnsFalse()
    {
        var handler = new SetGrowthSkillsInProgressCommandHandler(new FakeGrowthRepository(), new FakeUnitOfWork());
        bool ok = await handler.Handle(
            new SetGrowthSkillsInProgressCommand(Guid.NewGuid(), ["xUnit"]),
            CancellationToken.None);
        Assert.False(ok);
    }

    [Fact]
    public async Task Handle_TrimsDeduplicatesPreservesOrder()
    {
        var growth = new FakeGrowthRepository();
        Guid id = Guid.NewGuid();
        growth.Seed(new Domain.Entities.Growth { Id = id, TeamMemberId = Guid.NewGuid() });

        var handler = new SetGrowthSkillsInProgressCommandHandler(growth, new FakeUnitOfWork());
        bool ok = await handler.Handle(
            new SetGrowthSkillsInProgressCommand(id, ["  xUnit ", "Integration", "xunit", "", "EF Core"]),
            CancellationToken.None);

        Assert.True(ok);
        Domain.Entities.Growth? plan = await growth.GetByIdWithDetailsAsync(id, CancellationToken.None);
        Assert.NotNull(plan);
        Assert.Equal(["xUnit", "Integration", "EF Core"], plan.SkillsInProgress.OrderBy(x => x.SortOrder).Select(x => x.Value).ToList());
    }

    [Fact]
    public async Task Handle_RemovesStaleSkills()
    {
        var growth = new FakeGrowthRepository();
        Guid id = Guid.NewGuid();
        var plan = new Domain.Entities.Growth
        {
            Id = id,
            TeamMemberId = Guid.NewGuid(),
            SkillsInProgress =
            [
                new GrowthSkillInProgress { GrowthId = id, Value = "Old", SortOrder = 0 },
                new GrowthSkillInProgress { GrowthId = id, Value = "Keep", SortOrder = 1 }
            ]
        };
        growth.Seed(plan);

        var handler = new SetGrowthSkillsInProgressCommandHandler(growth, new FakeUnitOfWork());
        await handler.Handle(new SetGrowthSkillsInProgressCommand(id, ["Keep", "New"]), CancellationToken.None);

        Domain.Entities.Growth? loaded = await growth.GetByIdWithDetailsAsync(id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.SkillsInProgress.Count);
        Assert.DoesNotContain(loaded.SkillsInProgress, x => x.Value == "Old");
        Assert.Contains(loaded.SkillsInProgress, x => x.Value == "New");
    }
}
