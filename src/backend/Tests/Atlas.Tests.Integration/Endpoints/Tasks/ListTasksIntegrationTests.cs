using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Tasks;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Integration.Endpoints.Tasks;

public sealed class ListTasksIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly HttpClient _client;

    public ListTasksIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListTasks_WithIdsFilter_PersistsAcrossRequests()
    {
        CreateTaskResponse? kept = await CreateTaskAsync($"Keep-{Guid.NewGuid():N}");
        await CreateTaskAsync($"Skip-{Guid.NewGuid():N}");
        Assert.NotNull(kept);

        IReadOnlyList<TaskDto>? filtered = await (await _client.GetAsync($"/tasks?ids={kept.Id}"))
            .ReadJsonAsync<IReadOnlyList<TaskDto>>();

        Assert.NotNull(filtered);
        Assert.Single(filtered);
        Assert.Equal(kept.Id, filtered[0].Id);
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

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync<CreateTaskResponse>();
    }
}
