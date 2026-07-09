using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Tasks;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Tests.Integration.Workflows;

public sealed class TaskDependencyWorkflowIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    [Fact]
    public async Task CreateDependencyChain_ThenCycleAttempt_ReturnsBadRequest()
    {
        using AtlasIntegrationApplicationFactory factory = new();
        HttpClient client = factory.CreateClient();

        CreateTaskResponse? taskA = await CreateTaskAsync(client, "A");
        CreateTaskResponse? taskB = await CreateTaskAsync(client, "B");
        CreateTaskResponse? taskC = await CreateTaskAsync(client, "C");
        Assert.NotNull(taskA);
        Assert.NotNull(taskB);
        Assert.NotNull(taskC);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutJsonAsync(
            $"/tasks/{taskB.Id}",
            Update(taskB.Id, "B", [taskA.Id]))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutJsonAsync(
            $"/tasks/{taskC.Id}",
            Update(taskC.Id, "C", [taskB.Id]))).StatusCode);

        TaskDto? loadedC = await (await client.GetAsync($"/tasks/{taskC.Id}")).ReadJsonAsync<TaskDto>();
        Assert.NotNull(loadedC);
        Assert.Contains(taskB.Id, loadedC.DependencyTaskIds);

        HttpResponseMessage cycle = await client.PutJsonAsync(
            $"/tasks/{taskA.Id}",
            Update(taskA.Id, "A", [taskC.Id]));
        Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);
    }

    [Fact]
    public async Task CreateDependency_PersistsRowInTaskDependencies()
    {
        using AtlasIntegrationApplicationFactory factory = new();
        HttpClient client = factory.CreateClient();

        CreateTaskResponse? dependent = await CreateTaskAsync(client, "Dependent");
        CreateTaskResponse? blocker = await CreateTaskAsync(client, "Blocker");
        Assert.NotNull(dependent);
        Assert.NotNull(blocker);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutJsonAsync(
            $"/tasks/{dependent.Id}",
            Update(dependent.Id, "Dependent", [blocker.Id]))).StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        TaskDependency? row = await db.TaskDependencies
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.DependentTaskId == dependent.Id && d.BlockerTaskId == blocker.Id);

        Assert.NotNull(row);
        Assert.NotEqual(Guid.Empty, row.Id);
    }

    private static async Task<CreateTaskResponse?> CreateTaskAsync(HttpClient client, string title)
    {
        return await (await client.PostJsonAsync("/tasks", new CreateTaskRequest(
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
            Notes: string.Empty))).ReadJsonAsync<CreateTaskResponse>();
    }

    private static UpdateTaskRequest Update(Guid id, string title, IReadOnlyList<Guid> blockers) => new(
        Title: title,
        Priority: Priority.Medium,
        Status: Domain.Enums.TaskStatus.NotStarted,
        AssigneeId: null,
        ProjectId: null,
        RiskId: null,
        DueDate: null,
        DependencyTaskIds: blockers,
        EstimatedDurationText: "1h",
        EstimateConfidence: Confidence.Medium,
        ActualDurationText: null,
        Notes: string.Empty);
}
