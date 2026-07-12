using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.Risks.UpdateTeamMemberRisk;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.Risks;

public sealed class UpdateTeamMemberRiskCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateTeamMemberRiskCommandHandler _handler;

    public UpdateTeamMemberRiskCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateTeamMemberRiskCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenRiskExists_Updates()
    {
        var memberId = Guid.NewGuid();
        var riskId = Guid.NewGuid();
        var risk = new TeamMemberRisk
        {
            Id = riskId,
            TeamMemberId = memberId,
            Title = "Old",
            Severity = TeamMemberRiskSeverity.Low,
            RiskType = "T",
            Status = TeamMemberRiskStatus.Open,
            Trend = TeamMemberRiskTrend.Stable,
            FirstNoticedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ImpactArea = "A",
            Description = "D",
            CurrentAction = "C"
        };
        var member = new TeamMember { Id = memberId, Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "", Risks = [risk] };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new UpdateTeamMemberRiskCommand(
            memberId, riskId, "New", TeamMemberRiskSeverity.High, "Load", TeamMemberRiskStatus.Mitigating,
            TeamMemberRiskTrend.Improving, risk.FirstNoticedDate, "Delivery", "Desc", "Act", null, DateTimeOffset.UtcNow), CancellationToken.None);

        ok.Should().BeTrue();
        risk.Title.Should().Be("New");
        risk.Severity.Should().Be(TeamMemberRiskSeverity.High);
    }

    [Fact]
    public async Task Handle_WhenRiskMissing_ReturnsFalse()
    {
        var memberId = Guid.NewGuid();
        var member = new TeamMember { Id = memberId, Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "" };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new UpdateTeamMemberRiskCommand(
            memberId, Guid.NewGuid(), "T", TeamMemberRiskSeverity.Low, "T", TeamMemberRiskStatus.Open,
            TeamMemberRiskTrend.Stable, DateOnly.FromDateTime(DateTime.UtcNow), "A", "D", "C", null, null), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
