using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using FluentAssertions;

namespace Atlas.Ui.Tests.Mapping;

public sealed class EntityRequestMappersTests
{
    [Fact]
    public void ToUpdateTaskRequest_UsesStableProjectAndRiskIds()
    {
        var projectId = Guid.NewGuid();
        var riskId = Guid.NewGuid();
        AtlasTask task = new()
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            Priority = Priority.Medium,
            ProjectId = projectId,
            Project = "Ignored duplicate name",
            RiskId = riskId,
            Risk = "Ignored duplicate title"
        };

        AtlasApiDTOsTasksUpdateTaskRequest request = EntityRequestMappers.ToUpdateTaskRequest(task);

        request.ProjectId.Should().Be(projectId);
        request.RiskId.Should().Be(riskId);
    }

    [Fact]
    public void ToUpdateTaskRequest_WhenIdsMissing_DoesNotResolveFromDisplayNames()
    {
        Project[] projects =
        [
            new Project { Id = Guid.NewGuid(), Name = "Alpha", Summary = "" }
        ];
        Risk[] risks =
        [
            new Risk { Id = Guid.NewGuid(), Title = "Risk A", Status = RiskStatus.Open, Severity = "Low" }
        ];
        AtlasTask task = new()
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            Priority = Priority.Medium,
            Project = "Alpha",
            Risk = "Risk A"
        };

        AtlasApiDTOsTasksUpdateTaskRequest request = EntityRequestMappers.ToUpdateTaskRequest(task);

        request.ProjectId.Should().BeNull();
        request.RiskId.Should().BeNull();
    }

    [Fact]
    public void ToUpdateRiskRequest_UsesStableProjectId()
    {
        var projectId = Guid.NewGuid();
        Risk risk = new()
        {
            Id = Guid.NewGuid(),
            Title = "Risk",
            Status = RiskStatus.Open,
            Severity = "Low",
            ProjectId = projectId,
            Project = "Display only"
        };

        AtlasApiDTOsRisksUpdateRiskRequest request = EntityRequestMappers.ToUpdateRiskRequest(risk);

        request.ProjectId.Should().Be(projectId);
    }
}
