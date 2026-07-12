using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.Notes.AddTeamNote;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.Notes;

public sealed class AddTeamNoteCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly AddTeamNoteCommandHandler _handler;

    public AddTeamNoteCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new AddTeamNoteCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenMemberExists_AddsNote()
    {
        var member = new TeamMember { Id = Guid.NewGuid(), Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "" };
        _team.Setup(t => t.GetByIdAsync(member.Id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        TeamNote? captured = null;
        _team.Setup(t => t.AddNoteAsync(It.IsAny<TeamNote>(), It.IsAny<CancellationToken>()))
            .Callback<TeamNote, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);

        Guid id = await _handler.Handle(
            new AddTeamNoteCommand(member.Id, NoteType.Standup, "Title", "Text", "12345", "https://dev.azure.com/org/project/_git/repo/pullrequest/1"),
            CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured!.Text.Should().Be("Text");
        captured.Type.Should().Be(NoteType.Standup);
        captured.AdoWorkItemId.Should().Be("12345");
        captured.PrUrl.Should().Be("https://dev.azure.com/org/project/_git/repo/pullrequest/1");
    }

    [Fact]
    public async Task Handle_WhenMemberMissing_ReturnsEmptyGuid()
    {
        _team.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        Guid id = await _handler.Handle(new AddTeamNoteCommand(Guid.NewGuid(), NoteType.Quick, null, "x"), CancellationToken.None);

        id.Should().Be(Guid.Empty);
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
