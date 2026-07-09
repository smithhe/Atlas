using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.CreateTeamMember;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers;

public sealed class CreateTeamMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesMemberAndReturnsId()
    {
        var team = new Mock<ITeamMemberRepository>();
        (Mock<IUnitOfWork> uow, Mock<IUnitOfWorkTransaction> tx) = MockUnitOfWork.Create();
        TeamMember? captured = null;
        team.Setup(t => t.AddAsync(It.IsAny<TeamMember>(), It.IsAny<CancellationToken>()))
            .Callback<TeamMember, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var handler = new CreateTeamMemberCommandHandler(team.Object, uow.Object);
        Guid id = await handler.Handle(new CreateTeamMemberCommand("Ada", "Engineer", StatusDot.Green), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Ada");
        captured.Role.Should().Be("Engineer");
        tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
