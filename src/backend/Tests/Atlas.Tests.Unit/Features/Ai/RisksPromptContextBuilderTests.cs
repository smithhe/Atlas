using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Ai.Context;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class RisksPromptContextBuilderTests
{
    [Fact]
    public async Task BuildContextAsync_IncludesSelectedRiskAndActiveSnapshot()
    {
        var riskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var risk = new Risk
        {
            Id = riskId,
            Title = "Latency spike",
            Status = RiskStatus.Watching,
            Severity = SeverityLevel.High,
            ProjectId = projectId,
            Description = "P95 climbed after deploy",
            Evidence = "Grafana panel",
            Tasks =
            [
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Roll back canary",
                    Priority = Priority.High,
                    Status = Domain.Enums.TaskStatus.InProgress
                }
            ]
        };

        var risks = new Mock<IRiskRepository>();
        risks.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Risk> { risk });
        risks.Setup(r => r.GetByIdWithDetailsAsync(riskId, It.IsAny<CancellationToken>())).ReturnsAsync(risk);

        var projects = new Mock<IProjectRepository>();
        projects.Setup(p => p.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { new() { Id = projectId, Name = "Atlas" } });

        var builder = new RisksPromptContextBuilder(risks.Object, projects.Object);
        var context = await builder.BuildContextAsync(
            new AiSessionStartRequest(Guid.NewGuid(), 0, "help", AiViewScope.Risks, null, null, null, riskId, null),
            CancellationToken.None);

        context.Should().StartWith("Risks context:");
        context.Should().Contain("Latency spike");
        context.Should().Contain("Atlas");
        context.Should().Contain("Roll back canary");
        context.Should().Contain("Active risks snapshot:");
    }
}
