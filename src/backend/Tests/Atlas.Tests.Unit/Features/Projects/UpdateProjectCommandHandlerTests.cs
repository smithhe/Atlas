using Atlas.Application.DTOs;
using Atlas.Application.Features.Projects.UpdateProject;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.Projects;

public sealed class UpdateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProjectMissing_ReturnsFalse()
    {
        var handler = new UpdateProjectCommandHandler(new FakeProjectRepository(), new FakeUnitOfWork());
        bool ok = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);
        Assert.False(ok);
    }

    [Fact]
    public async Task Handle_SyncsTags_RemovesStaleAddsNew()
    {
        var projects = new FakeProjectRepository();
        Guid id = Guid.NewGuid();
        projects.Seed(new Project
        {
            Id = id,
            Name = "Atlas",
            Summary = "Summary",
            Tags =
            [
                new ProjectTag { ProjectId = id, Value = "old" },
                new ProjectTag { ProjectId = id, Value = "keep" }
            ]
        });

        var handler = new UpdateProjectCommandHandler(projects, new FakeUnitOfWork());
        bool ok = await handler.Handle(
            ValidCommand(id) with { Tags = ["keep", "new", " KEEP "] },
            CancellationToken.None);

        Assert.True(ok);
        Project? project = await projects.GetByIdWithDetailsAsync(id, CancellationToken.None);
        Assert.NotNull(project);
        Assert.Equal(2, project.Tags.Count);
        Assert.Contains(project.Tags, t => t.Value == "keep");
        Assert.Contains(project.Tags, t => t.Value == "new");
        Assert.DoesNotContain(project.Tags, t => t.Value == "old");
    }

    [Fact]
    public async Task Handle_SyncsLinks_CaseInsensitiveDedupe()
    {
        var projects = new FakeProjectRepository();
        Guid id = Guid.NewGuid();
        projects.Seed(new Project
        {
            Id = id,
            Name = "Atlas",
            Summary = "Summary",
            Links =
            [
                new ProjectLinkItem { ProjectId = id, Label = "Docs", Url = "https://example.com/docs" }
            ]
        });

        var handler = new UpdateProjectCommandHandler(projects, new FakeUnitOfWork());
        await handler.Handle(
            ValidCommand(id) with
            {
                Links =
                [
                    new ProjectLinkDto("Docs", "https://example.com/docs"),
                    new ProjectLinkDto("docs", "HTTPS://EXAMPLE.COM/DOCS"),
                    new ProjectLinkDto("Wiki", "https://example.com/wiki")
                ]
            },
            CancellationToken.None);

        Project? project = await projects.GetByIdWithDetailsAsync(id, CancellationToken.None);
        Assert.NotNull(project);
        Assert.Equal(2, project.Links.Count);
        Assert.Contains(project.Links, l => l.Label == "Wiki");
    }

    private static UpdateProjectCommand ValidCommand(Guid id) => new(
        Id: id,
        Name: "Atlas",
        Summary: "Summary",
        Description: null,
        Status: ProjectStatus.Active,
        Health: HealthSignal.Green,
        TargetDate: null,
        Priority: Priority.Medium,
        ProductOwnerId: null,
        Tags: null,
        Links: null);
}
