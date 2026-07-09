using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.Risks.AddTeamMemberRisk;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.Risks;

public sealed class AddTeamMemberRiskCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly AddTeamMemberRiskCommandHandler _handler;

    public AddTeamMemberRiskCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new AddTeamMemberRiskCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenMemberExists_AddsRisk()
    {
        var member = new TeamMember { Id = Guid.NewGuid(), Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "" };
        _team.Setup(t => t.GetByIdAsync(member.Id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        TeamMemberRisk? captured = null;
        _team.Setup(t => t.AddRiskAsync(It.IsAny<TeamMemberRisk>(), It.IsAny<CancellationToken>()))
            .Callback<TeamMemberRisk, CancellationToken>((r, _) => captured = r)
            .Returns(Task.CompletedTask);

        Guid id = await _handler.Handle(new AddTeamMemberRiskCommand(
            member.Id, "Burnout", TeamMemberRiskSeverity.High, "Load", TeamMemberRiskStatus.Open,
            TeamMemberRiskTrend.Worsening, DateOnly.FromDateTime(DateTime.UtcNow), "Delivery", "Desc", "Action", null), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured!.Title.Should().Be("Burnout");
    }

    [Fact]
    public async Task Handle_WhenMemberMissing_ReturnsEmptyGuid()
    {
        _team.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        Guid id = await _handler.Handle(new AddTeamMemberRiskCommand(
            Guid.NewGuid(), "T", TeamMemberRiskSeverity.Low, "Type", TeamMemberRiskStatus.Open,
            TeamMemberRiskTrend.Stable, DateOnly.FromDateTime(DateTime.UtcNow), "A", "D", "C", null), CancellationToken.None);

        id.Should().Be(Guid.Empty);
    }
}
