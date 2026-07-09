using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Tasks;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Integration.Endpoints.Tasks;

public sealed class TasksIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly HttpClient _client;

    public TasksIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HttpPipeline_CreateAndListTask_WiresEndpointPersistenceAndMediatR()
    {
        string title = $"Pipeline-{Guid.NewGuid():N}";

        CreateTaskRequest createRequest = new(
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

        HttpResponseMessage createResponse = await _client.PostJsonAsync("/tasks", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateTaskResponse? created = await createResponse.ReadJsonAsync<CreateTaskResponse>();
        Assert.NotNull(created);

        HttpResponseMessage getResponse = await _client.GetAsync($"/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        Atlas.Api.DTOs.Tasks.TaskDto? task = await getResponse.ReadJsonAsync<Atlas.Api.DTOs.Tasks.TaskDto>();
        Assert.NotNull(task);
        Assert.Equal(created.Id, task.Id);
        Assert.Equal(title, task.Title);
    }
}
