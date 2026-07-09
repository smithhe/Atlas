using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Tasks;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.Tasks;

public sealed class ListTasksEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ListTasksEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListTasks_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/tasks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListTasks_WithIdsFilter_ReturnsOnlyMatchingTasks()
    {
        CreateTaskResponse? first = await CreateTaskAsync($"First-{Guid.NewGuid():N}");
        CreateTaskResponse? second = await CreateTaskAsync($"Second-{Guid.NewGuid():N}");
        Assert.NotNull(first);
        Assert.NotNull(second);

        HttpResponseMessage response = await _client.GetAsync($"/tasks?ids={first.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        IReadOnlyList<TaskDto>? tasks = await response.ReadJsonAsync<IReadOnlyList<TaskDto>>();
        Assert.NotNull(tasks);
        Assert.Single(tasks);
        Assert.Equal(first.Id, tasks[0].Id);
    }

    [Fact]
    public async Task ListTasks_WithUnknownIds_ReturnsEmptyList()
    {
        HttpResponseMessage response = await _client.GetAsync($"/tasks?ids={Guid.NewGuid()}");
        IReadOnlyList<TaskDto>? tasks = await response.ReadJsonAsync<IReadOnlyList<TaskDto>>();

        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    private async Task<CreateTaskResponse?> CreateTaskAsync(string title)
    {
        HttpResponseMessage response = await _client.PostJsonAsync("/tasks", new CreateTaskRequest(
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
            Notes: string.Empty));

        return await response.ReadJsonAsync<CreateTaskResponse>();
    }
}
