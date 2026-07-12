using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Projects.TeamMembers.SetProjectTeamMembers;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Projects.TeamMembers;

public sealed class SetProjectTeamMembersCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly SetProjectTeamMembersCommandHandler _handler;

    public SetProjectTeamMembersCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new SetProjectTeamMembersCommandHandler(_projects.Object, _team.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenProjectMissing_ReturnsFalse()
    {
        _projects.Setup(p => p.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        bool ok = await _handler.Handle(new SetProjectTeamMembersCommand(Guid.NewGuid(), [Guid.NewGuid()]), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTeamMemberMissing_Throws()
    {
        var projectId = Guid.NewGuid();
        var missingMember = Guid.NewGuid();
        var project = new Project { Id = projectId, Name = "P", Summary = "S" };
        _projects.Setup(p => p.GetByIdWithDetailsAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _team.Setup(t => t.ExistsAsync(missingMember, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Func<Task> act = () => _handler.Handle(new SetProjectTeamMembersCommand(projectId, [missingMember]), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*'{missingMember}'*");
    }

    [Fact]
    public async Task Handle_WhenValid_SyncsMembership()
    {
        var projectId = Guid.NewGuid();
        var keep = Guid.NewGuid();
        var add = Guid.NewGuid();
        var remove = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Name = "P",
            Summary = "S",
            TeamMembers =
            [
                new ProjectTeamMember { ProjectId = projectId, TeamMemberId = keep },
                new ProjectTeamMember { ProjectId = projectId, TeamMemberId = remove }
            ]
        };
        _projects.Setup(p => p.GetByIdWithDetailsAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _team.Setup(t => t.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        bool ok = await _handler.Handle(new SetProjectTeamMembersCommand(projectId, [keep, add]), CancellationToken.None);

        ok.Should().BeTrue();
        project.TeamMembers.Select(x => x.TeamMemberId).Should().BeEquivalentTo([keep, add]);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
