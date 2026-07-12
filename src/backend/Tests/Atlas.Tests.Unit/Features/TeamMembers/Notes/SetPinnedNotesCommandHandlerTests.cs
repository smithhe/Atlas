using Atlas.Application.Features.TeamMembers.Notes.SetPinnedNotes;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.TeamMembers.Notes;

public sealed class SetPinnedNotesCommandHandlerTests
{
    [Fact]
    public async Task Handle_AssignsPinnedOrderByIndex()
    {
        var team = new FakeTeamMemberRepository();
        var memberId = Guid.NewGuid();
        var noteA = Guid.NewGuid();
        var noteB = Guid.NewGuid();
        var noteC = Guid.NewGuid();

        team.Seed(new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Engineer",
            StatusDot = StatusDot.Green,
            Notes =
            [
                NewNote(noteA, memberId),
                NewNote(noteB, memberId),
                NewNote(noteC, memberId)
            ]
        });

        var handler = new SetPinnedNotesCommandHandler(team, new FakeUnitOfWork());
        bool ok = await handler.Handle(
            new SetPinnedNotesCommand(memberId, [noteB, noteA]),
            CancellationToken.None);

        Assert.True(ok);
        TeamMember? member = await team.GetByIdWithDetailsAsync(memberId, CancellationToken.None);
        Assert.NotNull(member);
        Assert.Equal(0, member.Notes.Single(n => n.Id == noteB).PinnedOrder);
        Assert.Equal(1, member.Notes.Single(n => n.Id == noteA).PinnedOrder);
        Assert.Null(member.Notes.Single(n => n.Id == noteC).PinnedOrder);
    }

    [Fact]
    public async Task Handle_WhenNoteIdUnknown_ReturnsFalse()
    {
        var team = new FakeTeamMemberRepository();
        var memberId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        team.Seed(new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Engineer",
            StatusDot = StatusDot.Green,
            Notes = [NewNote(noteId, memberId)]
        });

        var handler = new SetPinnedNotesCommandHandler(team, new FakeUnitOfWork());
        bool ok = await handler.Handle(
            new SetPinnedNotesCommand(memberId, [noteId, Guid.NewGuid()]),
            CancellationToken.None);

        Assert.False(ok);
    }

    [Fact]
    public async Task Handle_WhenMemberMissing_ReturnsFalse()
    {
        var handler = new SetPinnedNotesCommandHandler(new FakeTeamMemberRepository(), new FakeUnitOfWork());
        bool ok = await handler.Handle(
            new SetPinnedNotesCommand(Guid.NewGuid(), [Guid.NewGuid()]),
            CancellationToken.None);
        Assert.False(ok);
    }

    private static TeamNote NewNote(Guid id, Guid memberId) => new()
    {
        Id = id,
        TeamMemberId = memberId,
        CreatedAt = DateTimeOffset.UtcNow,
        Type = NoteType.Quick,
        Text = "note",
        PinnedOrder = null
    };
}
