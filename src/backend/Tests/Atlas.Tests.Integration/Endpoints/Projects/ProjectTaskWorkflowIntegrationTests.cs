using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Projects;
using Atlas.Api.DTOs.Tasks;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Integration.Endpoints.Projects;

public sealed class ProjectTaskWorkflowIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly HttpClient _client;

    public ProjectTaskWorkflowIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProjectCreateTaskUpdateAndFilter_WorksEndToEnd()
    {
        CreateProjectResponse? project = await (await _client.PostJsonAsync("/projects", new CreateProjectRequest(
            Name: $"Workflow-{Guid.NewGuid():N}",
            Summary: "Integration project",
            Description: "desc",
            Status: ProjectStatus.Active,
            Health: HealthSignal.Green,
            TargetDate: null,
            Priority: Priority.High,
            ProductOwnerId: null,
            Tags: ["integration"],
            Links: null))).ReadJsonAsync<CreateProjectResponse>();
        Assert.NotNull(project);

        string title = $"Task-{Guid.NewGuid():N}";
        CreateTaskResponse? task = await (await _client.PostJsonAsync("/tasks", new CreateTaskRequest(
            Title: title,
            Priority: Priority.Medium,
            Status: Domain.Enums.TaskStatus.NotStarted,
            AssigneeId: null,
            ProjectId: project.Id,
            RiskId: null,
            DueDate: null,
            DependencyTaskIds: null,
            EstimatedDurationText: "3h",
            EstimateConfidence: Confidence.Medium,
            ActualDurationText: null,
            Notes: "workflow"))).ReadJsonAsync<CreateTaskResponse>();
        Assert.NotNull(task);

        HttpResponseMessage updateResponse = await _client.PutJsonAsync($"/tasks/{task.Id}", new UpdateTaskRequest(
            Title: $"{title}-done",
            Priority: Priority.High,
            Status: Domain.Enums.TaskStatus.Done,
            AssigneeId: null,
            ProjectId: project.Id,
            RiskId: null,
            DueDate: null,
            DependencyTaskIds: null,
            EstimatedDurationText: "3h",
            EstimateConfidence: Confidence.High,
            ActualDurationText: "2h",
            Notes: "completed"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        TaskDto? loaded = await (await _client.GetAsync($"/tasks/{task.Id}")).ReadJsonAsync<TaskDto>();
        Assert.NotNull(loaded);
        Assert.Equal(project.Id, loaded.ProjectId);
        Assert.Equal(Domain.Enums.TaskStatus.Done, loaded.Status);

        IReadOnlyList<TaskDto>? listed = await (await _client.GetAsync($"/tasks?ids={task.Id}")).ReadJsonAsync<IReadOnlyList<TaskDto>>();
        Assert.NotNull(listed);
        Assert.Single(listed);
        Assert.Equal($"{title}-done", listed[0].Title);
    }
}
