using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.AzureWorkItems.AddAzureWorkItemLocalNote;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.AzureWorkItems;

public sealed class AddAzureWorkItemLocalNoteCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly AddAzureWorkItemLocalNoteCommandHandler _handler;

    public AddAzureWorkItemLocalNoteCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new AddAzureWorkItemLocalNoteCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenLinkedWorkItem_AddsNote()
    {
        Guid memberId = Guid.NewGuid();
        var workItem = new AzureWorkItem { Id = Guid.NewGuid(), WorkItemId = 42, Title = "WI", State = "Active", WorkItemType = "Bug", AreaPath = "A", IterationPath = "I", ChangedDateUtc = DateTimeOffset.UtcNow, Url = "https://example.com" };
        var member = new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Eng",
            StatusDot = StatusDot.Green,
            CurrentFocus = "",
            AzureWorkItemLinks = [new AzureWorkItemLink { Id = Guid.NewGuid(), TeamMemberId = memberId, AzureWorkItemId = workItem.Id, AzureWorkItem = workItem }]
        };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        Guid id = await _handler.Handle(new AddAzureWorkItemLocalNoteCommand(memberId, 42, "Local note"), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        member.AzureWorkItemLocalNotes.Should().ContainSingle().Which.Text.Should().Be("Local note");
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotLinked_ReturnsEmptyGuid()
    {
        Guid memberId = Guid.NewGuid();
        var member = new TeamMember { Id = memberId, Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "" };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        Guid id = await _handler.Handle(new AddAzureWorkItemLocalNoteCommand(memberId, 99, "x"), CancellationToken.None);

        id.Should().Be(Guid.Empty);
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
