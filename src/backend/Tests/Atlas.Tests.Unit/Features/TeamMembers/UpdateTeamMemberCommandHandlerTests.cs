using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.UpdateTeamMember;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers;

public sealed class UpdateTeamMemberCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateTeamMemberCommandHandler _handler;

    public UpdateTeamMemberCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateTeamMemberCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenExists_UpdatesFields()
    {
        var member = new TeamMember { Id = Guid.NewGuid(), Name = "Old", Role = "R", StatusDot = StatusDot.Yellow, CurrentFocus = "A" };
        _team.Setup(t => t.GetByIdAsync(member.Id, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new UpdateTeamMemberCommand(member.Id, "New", "Lead", StatusDot.Green, "Focus"), CancellationToken.None);

        ok.Should().BeTrue();
        member.Name.Should().Be("New");
        member.Role.Should().Be("Lead");
        member.CurrentFocus.Should().Be("Focus");
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _team.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        bool ok = await _handler.Handle(new UpdateTeamMemberCommand(Guid.NewGuid(), "N", null, StatusDot.Green, ""), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
