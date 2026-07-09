using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.Risks.DeleteTeamMemberRisk;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.Risks;

public sealed class DeleteTeamMemberRiskCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteTeamMemberRiskCommandHandler _handler;

    public DeleteTeamMemberRiskCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteTeamMemberRiskCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenRiskExists_Removes()
    {
        Guid memberId = Guid.NewGuid();
        Guid riskId = Guid.NewGuid();
        var member = new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Eng",
            StatusDot = StatusDot.Green,
            CurrentFocus = "",
            Risks =
            [
                new TeamMemberRisk
                {
                    Id = riskId,
                    TeamMemberId = memberId,
                    Title = "R",
                    Severity = TeamMemberRiskSeverity.Low,
                    RiskType = "T",
                    Status = TeamMemberRiskStatus.Open,
                    Trend = TeamMemberRiskTrend.Stable,
                    FirstNoticedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    ImpactArea = "A",
                    Description = "D",
                    CurrentAction = "C"
                }
            ]
        };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new DeleteTeamMemberRiskCommand(memberId, riskId), CancellationToken.None);

        ok.Should().BeTrue();
        member.Risks.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _team.Setup(t => t.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        bool ok = await _handler.Handle(new DeleteTeamMemberRiskCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
