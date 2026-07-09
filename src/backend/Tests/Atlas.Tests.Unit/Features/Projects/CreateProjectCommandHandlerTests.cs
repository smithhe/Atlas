using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.DTOs;
using Atlas.Application.Features.Projects.CreateProject;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Projects;

public sealed class CreateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new CreateProjectCommandHandler(_projects.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesProjectWithTagsAndLinks()
    {
        Project? captured = null;
        _projects.Setup(p => p.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((project, _) => captured = project)
            .Returns(Task.CompletedTask);

        Guid id = await _handler.Handle(new CreateProjectCommand(
            Name: "Atlas",
            Summary: "Summary",
            Description: "Desc",
            Status: ProjectStatus.Active,
            Health: HealthSignal.Green,
            TargetDate: null,
            Priority: Priority.High,
            ProductOwnerId: null,
            Tags: [" alpha ", "Alpha", "beta"],
            Links:
            [
                new ProjectLinkDto("Docs", "https://example.com/docs"),
                new ProjectLinkDto("docs", "HTTPS://EXAMPLE.COM/DOCS")
            ]), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Atlas");
        captured.Tags.Select(t => t.Value).Should().BeEquivalentTo(["alpha", "beta"]);
        captured.Links.Should().ContainSingle();
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
