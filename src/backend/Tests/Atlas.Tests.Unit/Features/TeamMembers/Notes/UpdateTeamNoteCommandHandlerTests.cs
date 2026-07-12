using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.Notes.UpdateTeamNote;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.Notes;

public sealed class UpdateTeamNoteCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateTeamNoteCommandHandler _handler;

    public UpdateTeamNoteCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateTeamNoteCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenNoteExists_Updates()
    {
        var memberId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var note = new TeamNote { Id = noteId, TeamMemberId = memberId, Type = NoteType.Quick, Text = "Old", CreatedAt = DateTimeOffset.UtcNow };
        var member = new TeamMember { Id = memberId, Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "", Notes = [note] };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(
            new UpdateTeamNoteCommand(
                memberId,
                noteId,
                NoteType.Standup,
                "T",
                "New",
                0,
                "98765",
                "https://dev.azure.com/org/project/_git/repo/pullrequest/9"),
            CancellationToken.None);

        ok.Should().BeTrue();
        note.Text.Should().Be("New");
        note.Type.Should().Be(NoteType.Standup);
        note.PinnedOrder.Should().Be(0);
        note.AdoWorkItemId.Should().Be("98765");
        note.PrUrl.Should().Be("https://dev.azure.com/org/project/_git/repo/pullrequest/9");
    }

    [Fact]
    public async Task Handle_WhenNoteMissing_ReturnsFalse()
    {
        var memberId = Guid.NewGuid();
        var member = new TeamMember { Id = memberId, Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "" };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new UpdateTeamNoteCommand(memberId, Guid.NewGuid(), NoteType.Quick, null, "x", null), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
