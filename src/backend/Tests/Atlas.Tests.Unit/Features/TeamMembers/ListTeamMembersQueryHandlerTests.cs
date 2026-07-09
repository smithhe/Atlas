using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.ListTeamMembers;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers;

public sealed class ListTeamMembersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRepositoryList()
    {
        var team = new Mock<ITeamMemberRepository>();
        IReadOnlyList<TeamMember> expected = [];
        team.Setup(t => t.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new ListTeamMembersQueryHandler(team.Object);
        (await handler.Handle(new ListTeamMembersQuery(), CancellationToken.None)).Should().BeSameAs(expected);
    }
}
