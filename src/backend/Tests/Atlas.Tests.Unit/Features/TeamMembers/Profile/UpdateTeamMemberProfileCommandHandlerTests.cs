using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.Profile.UpdateTeamMemberProfile;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.Profile;

public sealed class UpdateTeamMemberProfileCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateTeamMemberProfileCommandHandler _handler;

    public UpdateTeamMemberProfileCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateTeamMemberProfileCommandHandler(_team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenExists_UpdatesProfileTrimmingBlanks()
    {
        var member = new TeamMember { Id = Guid.NewGuid(), Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "" };
        _team.Setup(t => t.GetByIdAsync(member.Id, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new UpdateTeamMemberProfileCommand(member.Id, " America/Chicago ", "  "), CancellationToken.None);

        ok.Should().BeTrue();
        member.Profile.TimeZone.Should().Be("America/Chicago");
        member.Profile.TypicalHours.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _team.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        bool ok = await _handler.Handle(new UpdateTeamMemberProfileCommand(Guid.NewGuid(), null, null), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
