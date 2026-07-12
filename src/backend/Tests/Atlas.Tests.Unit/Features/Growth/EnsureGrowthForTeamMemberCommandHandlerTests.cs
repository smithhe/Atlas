using Atlas.Application.Features.Growth.EnsureGrowthForTeamMember;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.Growth;

public sealed class EnsureGrowthForTeamMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPlanExists_ReturnsExistingId()
    {
        var growthRepo = new FakeGrowthRepository();
        var memberId = Guid.NewGuid();
        var existingId = Guid.NewGuid();
        growthRepo.Seed(new Atlas.Domain.Entities.Growth
        {
            Id = existingId,
            TeamMemberId = memberId,
            FocusAreasMarkdown = string.Empty
        });

        var handler = new EnsureGrowthForTeamMemberCommandHandler(growthRepo, new FakeUnitOfWork());
        Guid result = await handler.Handle(new EnsureGrowthForTeamMemberCommand(memberId), CancellationToken.None);

        Assert.Equal(existingId, result);
    }

    [Fact]
    public async Task Handle_WhenPlanMissing_CreatesNewPlan()
    {
        var growthRepo = new FakeGrowthRepository();
        var memberId = Guid.NewGuid();

        var handler = new EnsureGrowthForTeamMemberCommandHandler(growthRepo, new FakeUnitOfWork());
        Guid result = await handler.Handle(new EnsureGrowthForTeamMemberCommand(memberId), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Atlas.Domain.Entities.Growth? created = await growthRepo.GetByTeamMemberIdAsync(memberId, CancellationToken.None);
        Assert.NotNull(created);
        Assert.Equal(result, created.Id);
    }
}
