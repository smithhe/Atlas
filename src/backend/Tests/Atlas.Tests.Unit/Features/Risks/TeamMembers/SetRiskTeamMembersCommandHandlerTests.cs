using Atlas.Application.Features.Risks.TeamMembers.SetRiskTeamMembers;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.Risks.TeamMembers;

public sealed class SetRiskTeamMembersCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRiskMissing_ReturnsFalse()
    {
        var handler = new SetRiskTeamMembersCommandHandler(
            new FakeRiskRepository(),
            new FakeTeamMemberRepository(),
            new FakeUnitOfWork());

        bool ok = await handler.Handle(
            new SetRiskTeamMembersCommand(Guid.NewGuid(), [Guid.NewGuid()]),
            CancellationToken.None);

        Assert.False(ok);
    }

    [Fact]
    public async Task Handle_WhenMemberMissing_Throws()
    {
        var risks = new FakeRiskRepository();
        var riskId = Guid.NewGuid();
        risks.Seed(NewRisk(riskId));

        var handler = new SetRiskTeamMembersCommandHandler(
            risks,
            new FakeTeamMemberRepository(),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new SetRiskTeamMembersCommand(riskId, [Guid.NewGuid()]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenEmptyList_ClearsMembers()
    {
        var risks = new FakeRiskRepository();
        var team = new FakeTeamMemberRepository();
        var riskId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        team.Seed(new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Engineer",
            StatusDot = StatusDot.Green
        });

        Risk risk = NewRisk(riskId);
        risk.LinkedTeamMembers.Add(new RiskTeamMember
        {
            RiskId = riskId,
            TeamMemberId = memberId
        });
        risks.Seed(risk);

        var handler = new SetRiskTeamMembersCommandHandler(risks, team, new FakeUnitOfWork());
        bool ok = await handler.Handle(
            new SetRiskTeamMembersCommand(riskId, []),
            CancellationToken.None);

        Assert.True(ok);
        Risk? updated = await risks.GetByIdWithDetailsAsync(riskId, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Empty(updated.LinkedTeamMembers);
    }

    [Fact]
    public async Task Handle_WhenValid_AssignsMembers()
    {
        var risks = new FakeRiskRepository();
        var team = new FakeTeamMemberRepository();
        var riskId = Guid.NewGuid();
        var memberA = Guid.NewGuid();
        var memberB = Guid.NewGuid();

        risks.Seed(NewRisk(riskId));
        team.Seed(new TeamMember { Id = memberA, Name = "Ada", Role = "Engineer", StatusDot = StatusDot.Green });
        team.Seed(new TeamMember { Id = memberB, Name = "Grace", Role = "Engineer", StatusDot = StatusDot.Green });

        var handler = new SetRiskTeamMembersCommandHandler(risks, team, new FakeUnitOfWork());
        bool ok = await handler.Handle(
            new SetRiskTeamMembersCommand(riskId, [memberA, memberB]),
            CancellationToken.None);

        Assert.True(ok);
        Risk? updated = await risks.GetByIdWithDetailsAsync(riskId, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.LinkedTeamMembers.Count);
        Assert.Contains(updated.LinkedTeamMembers, x => x.TeamMemberId == memberA);
        Assert.Contains(updated.LinkedTeamMembers, x => x.TeamMemberId == memberB);
    }

    private static Risk NewRisk(Guid id) => new()
    {
        Id = id,
        Title = "Risk",
        Status = RiskStatus.Open,
        Severity = SeverityLevel.Medium,
        Description = "desc",
        Evidence = "evidence",
        LastUpdatedAt = DateTimeOffset.UtcNow
    };
}
