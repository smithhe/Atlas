using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.DeleteTeamMember;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers;

public sealed class DeleteTeamMemberCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteTeamMemberCommandHandler _handler;

    public DeleteTeamMemberCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteTeamMemberCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenExists_Removes()
    {
        var member = new TeamMember { Id = Guid.NewGuid(), Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "" };
        _team.Setup(t => t.GetByIdAsync(member.Id, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new DeleteTeamMemberCommand(member.Id), CancellationToken.None);

        ok.Should().BeTrue();
        _team.Verify(t => t.Remove(member), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _team.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        bool ok = await _handler.Handle(new DeleteTeamMemberCommand(Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
