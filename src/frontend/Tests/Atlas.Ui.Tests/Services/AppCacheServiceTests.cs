using Atlas.Ui.Api.Generated;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using FluentAssertions;
using Moq;

namespace Atlas.Ui.Tests.Services;

public sealed class AppCacheServiceTests
{
    [Fact]
    public void UpdateProject_WhenNameChanges_ReplacesLinkedTaskAndRiskProjectLabels()
    {
        var projectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();
        var linkedTaskId = Guid.NewGuid();
        var otherTaskId = Guid.NewGuid();
        var linkedRiskId = Guid.NewGuid();
        var otherRiskId = Guid.NewGuid();

        Project project = new() { Id = projectId, Name = "Atlas" };
        AppCacheService cache = AutosaveTestSupport.CreateCache(new Mock<IAtlasApiClient>().Object);
        cache.AddProject(project);
        cache.AddProject(new() { Id = otherProjectId, Name = "Other" });
        cache.AddTask(new()
        {
            Id = linkedTaskId,
            Title = "Linked task",
            ProjectId = projectId,
            Project = "Atlas"
        });
        cache.AddTask(new()
        {
            Id = otherTaskId,
            Title = "Other task",
            ProjectId = otherProjectId,
            Project = "Other"
        });
        cache.AddRisk(new()
        {
            Id = linkedRiskId,
            Title = "Linked risk",
            ProjectId = projectId,
            Project = "Atlas"
        });
        cache.AddRisk(new()
        {
            Id = otherRiskId,
            Title = "Other risk",
            ProjectId = otherProjectId,
            Project = "Other"
        });

        cache.UpdateProject(new() { Id = projectId, Name = "Atlas Core" });

        cache.TryGetTask(linkedTaskId)!.Project.Should().Be("Atlas Core");
        cache.TryGetRisk(linkedRiskId)!.Project.Should().Be("Atlas Core");
        cache.TryGetTask(otherTaskId)!.Project.Should().Be("Other");
        cache.TryGetRisk(otherRiskId)!.Project.Should().Be("Other");
    }

    [Fact]
    public void UpdateRisk_WhenTitleChanges_ReplacesLinkedTaskRiskLabel()
    {
        var riskId = Guid.NewGuid();
        var otherRiskId = Guid.NewGuid();
        var linkedTaskId = Guid.NewGuid();
        var otherTaskId = Guid.NewGuid();

        AppCacheService cache = AutosaveTestSupport.CreateCache(new Mock<IAtlasApiClient>().Object);
        cache.AddRisk(new() { Id = riskId, Title = "Old risk" });
        cache.AddRisk(new() { Id = otherRiskId, Title = "Other risk" });
        cache.AddTask(new()
        {
            Id = linkedTaskId,
            Title = "Linked task",
            RiskId = riskId,
            Risk = "Old risk"
        });
        cache.AddTask(new()
        {
            Id = otherTaskId,
            Title = "Other task",
            RiskId = otherRiskId,
            Risk = "Other risk"
        });

        cache.UpdateRisk(new() { Id = riskId, Title = "Renamed risk" });

        cache.TryGetTask(linkedTaskId)!.Risk.Should().Be("Renamed risk");
        cache.TryGetTask(otherTaskId)!.Risk.Should().Be("Other risk");
    }
}
