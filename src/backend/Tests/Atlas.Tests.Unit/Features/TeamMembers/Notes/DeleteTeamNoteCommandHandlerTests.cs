using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.Notes.DeleteTeamNote;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.Notes;

public sealed class DeleteTeamNoteCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteTeamNoteCommandHandler _handler;

    public DeleteTeamNoteCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteTeamNoteCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenNoteExists_RemovesAndRepacksPins()
    {
        var memberId = Guid.NewGuid();
        var keep = Guid.NewGuid();
        var remove = Guid.NewGuid();
        var member = new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Eng",
            StatusDot = StatusDot.Green,
            CurrentFocus = "",
            Notes =
            [
                new TeamNote { Id = remove, TeamMemberId = memberId, Type = NoteType.Quick, Text = "a", CreatedAt = DateTimeOffset.UtcNow, PinnedOrder = 0 },
                new TeamNote { Id = keep, TeamMemberId = memberId, Type = NoteType.Quick, Text = "b", CreatedAt = DateTimeOffset.UtcNow, PinnedOrder = 1 }
            ]
        };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new DeleteTeamNoteCommand(memberId, remove), CancellationToken.None);

        ok.Should().BeTrue();
        member.Notes.Should().ContainSingle().Which.Id.Should().Be(keep);
        member.Notes[0].PinnedOrder.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenMemberMissing_ReturnsFalse()
    {
        _team.Setup(t => t.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        bool ok = await _handler.Handle(new DeleteTeamNoteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
