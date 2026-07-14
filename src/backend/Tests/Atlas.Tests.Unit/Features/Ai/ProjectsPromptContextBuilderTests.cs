using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Ai.Context;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class ProjectsPromptContextBuilderTests
{
    [Fact]
    public async Task BuildContextAsync_IncludesSelectedProjectRelatedWorkAndSnapshot()
    {
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Name = "Atlas Cockpit",
            Summary = "EM cockpit",
            Status = ProjectStatus.Active,
            Health = HealthSignal.Yellow,
            Priority = Priority.High,
            Tasks =
            [
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Wire AI scopes",
                    Priority = Priority.Critical,
                    Status = Domain.Enums.TaskStatus.InProgress
                }
            ],
            Risks =
            [
                new Risk
                {
                    Id = Guid.NewGuid(),
                    Title = "Missing OpenAI key",
                    Severity = SeverityLevel.Medium,
                    Status = RiskStatus.Open
                }
            ]
        };

        var projects = new Mock<IProjectRepository>();
        projects.Setup(p => p.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Project> { project });
        projects.Setup(p => p.GetByIdWithDetailsAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var builder = new ProjectsPromptContextBuilder(projects.Object);
        var context = await builder.BuildContextAsync(
            new AiSessionStartRequest(Guid.NewGuid(), 0, "help", AiViewScope.Projects, null, null, projectId, null, null),
            CancellationToken.None);

        context.Should().StartWith("Projects context:");
        context.Should().Contain("Atlas Cockpit");
        context.Should().Contain("Wire AI scopes");
        context.Should().Contain("Missing OpenAI key");
        context.Should().Contain("Projects snapshot:");
    }
}
