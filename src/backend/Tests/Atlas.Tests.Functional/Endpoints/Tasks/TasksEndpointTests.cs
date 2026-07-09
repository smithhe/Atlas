using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Tasks;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.Tasks;

public sealed class TasksEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TasksEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGetUpdateDeleteTask_CompletesCrudFlow()
    {
        string title = $"Task-{Guid.NewGuid():N}";

        CreateTaskRequest createRequest = ValidCreateRequest(title);
        HttpResponseMessage createResponse = await _client.PostJsonAsync("/tasks", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateTaskResponse? created = await createResponse.ReadJsonAsync<CreateTaskResponse>();
        Assert.NotNull(created);

        HttpResponseMessage getResponse = await _client.GetAsync($"/tasks/{created.Id}");
        TaskDto? task = await getResponse.ReadJsonAsync<TaskDto>();
        Assert.NotNull(task);
        Assert.Equal(title, task.Title);

        UpdateTaskRequest updateRequest = new(
            Title: $"{title}-updated",
            Priority: Priority.High,
            Status: Domain.Enums.TaskStatus.InProgress,
            AssigneeId: null,
            ProjectId: null,
            RiskId: null,
            DueDate: null,
            DependencyTaskIds: null,
            EstimatedDurationText: "2h",
            EstimateConfidence: Confidence.High,
            ActualDurationText: "1h",
            Notes: "Updated");

        HttpResponseMessage updateResponse = await _client.PutJsonAsync($"/tasks/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage missingResponse = await _client.GetAsync($"/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithMissingBlocker_ReturnsBadRequest()
    {
        CreateTaskRequest request = ValidCreateRequest("Blocked") with
        {
            DependencyTaskIds = [Guid.NewGuid()]
        };

        HttpResponseMessage response = await _client.PostJsonAsync("/tasks", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithValidDependency_LinksBlocker()
    {
        CreateTaskResponse? blocker = await (await _client.PostJsonAsync("/tasks", ValidCreateRequest("Blocker"))).ReadJsonAsync<CreateTaskResponse>();
        Assert.NotNull(blocker);

        CreateTaskRequest dependentRequest = ValidCreateRequest("Dependent") with
        {
            DependencyTaskIds = [blocker.Id]
        };

        HttpResponseMessage createResponse = await _client.PostJsonAsync("/tasks", dependentRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateTaskResponse? dependent = await createResponse.ReadJsonAsync<CreateTaskResponse>();
        Assert.NotNull(dependent);

        TaskDto? task = await (await _client.GetAsync($"/tasks/{dependent.Id}")).ReadJsonAsync<TaskDto>();
        Assert.NotNull(task);
        Assert.Contains(blocker.Id, task.DependencyTaskIds);
    }

    [Fact]
    public async Task UpdateTask_WithDependencyCycle_ReturnsBadRequest()
    {
        CreateTaskResponse? taskA = await (await _client.PostJsonAsync("/tasks", ValidCreateRequest("A"))).ReadJsonAsync<CreateTaskResponse>();
        CreateTaskResponse? taskB = await (await _client.PostJsonAsync("/tasks", ValidCreateRequest("B"))).ReadJsonAsync<CreateTaskResponse>();
        Assert.NotNull(taskA);
        Assert.NotNull(taskB);

        HttpResponseMessage linkBToA = await _client.PutJsonAsync($"/tasks/{taskA.Id}", ValidUpdateRequest(taskA.Id, "A") with
        {
            DependencyTaskIds = [taskB.Id]
        });
        Assert.Equal(HttpStatusCode.NoContent, linkBToA.StatusCode);

        HttpResponseMessage cycle = await _client.PutJsonAsync($"/tasks/{taskB.Id}", ValidUpdateRequest(taskB.Id, "B") with
        {
            DependencyTaskIds = [taskA.Id]
        });
        Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_AddsAndRemovesBlockers()
    {
        CreateTaskResponse? blocker = await (await _client.PostJsonAsync("/tasks", ValidCreateRequest("Blocker"))).ReadJsonAsync<CreateTaskResponse>();
        CreateTaskResponse? dependent = await (await _client.PostJsonAsync("/tasks", ValidCreateRequest("Dependent"))).ReadJsonAsync<CreateTaskResponse>();
        Assert.NotNull(blocker);
        Assert.NotNull(dependent);

        HttpResponseMessage addBlocker = await _client.PutJsonAsync(
            $"/tasks/{dependent.Id}",
            ValidUpdateRequest(dependent.Id, "Dependent") with { DependencyTaskIds = [blocker.Id] });
        Assert.Equal(HttpStatusCode.NoContent, addBlocker.StatusCode);

        TaskDto? withBlocker = await (await _client.GetAsync($"/tasks/{dependent.Id}")).ReadJsonAsync<TaskDto>();
        Assert.NotNull(withBlocker);
        Assert.Contains(blocker.Id, withBlocker.DependencyTaskIds);

        HttpResponseMessage clearBlockers = await _client.PutJsonAsync(
            $"/tasks/{dependent.Id}",
            ValidUpdateRequest(dependent.Id, "Dependent") with { DependencyTaskIds = [] });
        Assert.Equal(HttpStatusCode.NoContent, clearBlockers.StatusCode);

        TaskDto? cleared = await (await _client.GetAsync($"/tasks/{dependent.Id}")).ReadJsonAsync<TaskDto>();
        Assert.NotNull(cleared);
        Assert.Empty(cleared.DependencyTaskIds);
    }

    [Fact]
    public async Task UpdateTask_WhenMissing_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.PutJsonAsync(
            $"/tasks/{Guid.NewGuid()}",
            ValidUpdateRequest(Guid.NewGuid(), "Missing"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CreateTaskRequest ValidCreateRequest(string title) => new(
        Title: title,
        Priority: Priority.Medium,
        Status: Domain.Enums.TaskStatus.NotStarted,
        AssigneeId: null,
        ProjectId: null,
        RiskId: null,
        DueDate: null,
        DependencyTaskIds: null,
        EstimatedDurationText: "1h",
        EstimateConfidence: Confidence.Medium,
        ActualDurationText: null,
        Notes: string.Empty);

    private static UpdateTaskRequest ValidUpdateRequest(Guid id, string title) => new(
        Title: title,
        Priority: Priority.Medium,
        Status: Domain.Enums.TaskStatus.NotStarted,
        AssigneeId: null,
        ProjectId: null,
        RiskId: null,
        DueDate: null,
        DependencyTaskIds: null,
        EstimatedDurationText: "1h",
        EstimateConfidence: Confidence.Medium,
        ActualDurationText: null,
        Notes: string.Empty);
}
