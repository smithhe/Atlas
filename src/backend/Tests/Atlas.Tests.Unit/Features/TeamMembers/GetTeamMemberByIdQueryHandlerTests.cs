using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.GetTeamMember;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers;

public sealed class GetTeamMemberByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenIncludeDetails_UsesDetails()
    {
        var team = new Mock<ITeamMemberRepository>();
        var id = Guid.NewGuid();
        var member = new TeamMember { Id = id, Name = "Ada", Role = "Eng", StatusDot = StatusDot.Green, CurrentFocus = "" };
        team.Setup(t => t.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        var handler = new GetTeamMemberByIdQueryHandler(team.Object);
        (await handler.Handle(new GetTeamMemberByIdQuery(id, true), CancellationToken.None)).Should().BeSameAs(member);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsNull()
    {
        var team = new Mock<ITeamMemberRepository>();
        team.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        var handler = new GetTeamMemberByIdQueryHandler(team.Object);
        (await handler.Handle(new GetTeamMemberByIdQuery(Guid.NewGuid(), false), CancellationToken.None)).Should().BeNull();
    }
}
