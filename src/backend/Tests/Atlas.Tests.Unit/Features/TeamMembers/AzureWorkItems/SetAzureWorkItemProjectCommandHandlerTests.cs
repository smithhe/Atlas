using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.TeamMembers.AzureWorkItems.SetAzureWorkItemProject;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.TeamMembers.AzureWorkItems;

public sealed class SetAzureWorkItemProjectCommandHandlerTests
{
    private readonly Mock<ITeamMemberRepository> _team = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly SetAzureWorkItemProjectCommandHandler _handler;

    public SetAzureWorkItemProjectCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new SetAzureWorkItemProjectCommandHandler(_team.Object, _projects.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenLinkedWorkItem_UpdatesProjectId()
    {
        var memberId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var workItem = new AzureWorkItem
        {
            Id = Guid.NewGuid(),
            WorkItemId = 42,
            Title = "WI",
            State = "Active",
            WorkItemType = "Bug",
            AreaPath = "A",
            IterationPath = "I",
            ChangedDateUtc = DateTimeOffset.UtcNow,
            Url = "https://example.com"
        };
        var link = new AzureWorkItemLink
        {
            Id = Guid.NewGuid(),
            TeamMemberId = memberId,
            AzureWorkItemId = workItem.Id,
            AzureWorkItem = workItem,
            ProjectId = Guid.Empty,
            LinkedAtUtc = DateTimeOffset.UtcNow
        };
        var member = new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Eng",
            StatusDot = StatusDot.Green,
            CurrentFocus = "",
            AzureWorkItemLinks = [link]
        };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _projects.Setup(p => p.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = projectId, Name = "Atlas", Summary = "Summary", LastUpdatedAt = DateTimeOffset.UtcNow });

        bool ok = await _handler.Handle(new SetAzureWorkItemProjectCommand(memberId, 42, projectId), CancellationToken.None);

        ok.Should().BeTrue();
        link.ProjectId.Should().Be(projectId);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectMissing_ReturnsFalse()
    {
        var memberId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var workItem = new AzureWorkItem
        {
            Id = Guid.NewGuid(),
            WorkItemId = 42,
            Title = "WI",
            State = "Active",
            WorkItemType = "Bug",
            AreaPath = "A",
            IterationPath = "I",
            ChangedDateUtc = DateTimeOffset.UtcNow,
            Url = "https://example.com"
        };
        var link = new AzureWorkItemLink
        {
            Id = Guid.NewGuid(),
            TeamMemberId = memberId,
            AzureWorkItemId = workItem.Id,
            AzureWorkItem = workItem,
            ProjectId = Guid.Empty,
            LinkedAtUtc = DateTimeOffset.UtcNow
        };
        var member = new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Eng",
            StatusDot = StatusDot.Green,
            CurrentFocus = "",
            AzureWorkItemLinks = [link]
        };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _projects.Setup(p => p.GetByIdAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        bool ok = await _handler.Handle(new SetAzureWorkItemProjectCommand(memberId, 42, projectId), CancellationToken.None);

        ok.Should().BeFalse();
        link.ProjectId.Should().Be(Guid.Empty);
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenClearingProject_SetsEmptyWithoutLookup()
    {
        var memberId = Guid.NewGuid();
        var workItem = new AzureWorkItem
        {
            Id = Guid.NewGuid(),
            WorkItemId = 42,
            Title = "WI",
            State = "Active",
            WorkItemType = "Bug",
            AreaPath = "A",
            IterationPath = "I",
            ChangedDateUtc = DateTimeOffset.UtcNow,
            Url = "https://example.com"
        };
        var link = new AzureWorkItemLink
        {
            Id = Guid.NewGuid(),
            TeamMemberId = memberId,
            AzureWorkItemId = workItem.Id,
            AzureWorkItem = workItem,
            ProjectId = Guid.NewGuid(),
            LinkedAtUtc = DateTimeOffset.UtcNow
        };
        var member = new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Eng",
            StatusDot = StatusDot.Green,
            CurrentFocus = "",
            AzureWorkItemLinks = [link]
        };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new SetAzureWorkItemProjectCommand(memberId, 42, null), CancellationToken.None);

        ok.Should().BeTrue();
        link.ProjectId.Should().Be(Guid.Empty);
        _projects.Verify(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotLinked_ReturnsFalse()
    {
        var memberId = Guid.NewGuid();
        var member = new TeamMember
        {
            Id = memberId,
            Name = "Ada",
            Role = "Eng",
            StatusDot = StatusDot.Green,
            CurrentFocus = ""
        };
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(member);

        bool ok = await _handler.Handle(new SetAzureWorkItemProjectCommand(memberId, 99, Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMemberMissing_ReturnsFalse()
    {
        var memberId = Guid.NewGuid();
        _team.Setup(t => t.GetByIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync((TeamMember?)null);

        bool ok = await _handler.Handle(new SetAzureWorkItemProjectCommand(memberId, 42, Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
