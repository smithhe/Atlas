using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Api.DTOs.Projects;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Helpers;

internal static class AzureDevOpsTestHelper
{
    public static async Task ConfigureConnectionAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PutJsonAsync(
            "/azure-devops/connection",
            new UpdateAzureConnectionRequest(
                Organization: "contoso",
                Project: "Atlas",
                AreaPath: "Atlas\\Core",
                TeamName: "Core",
                IsEnabled: true,
                ProjectId: "proj-1",
                TeamId: "team-1"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<AzureSyncResultDto> RunSyncAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsync("/azure-devops/sync", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AzureSyncResultDto? result = await response.ReadJsonAsync<AzureSyncResultDto>();
        Assert.NotNull(result);
        return result;
    }

    public static async Task<Guid> CreateProjectAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostJsonAsync(
            "/projects",
            new CreateProjectRequest(
                Name: $"ADO-{Guid.NewGuid():N}",
                Summary: "Summary",
                Description: null,
                Status: ProjectStatus.Active,
                Health: HealthSignal.Green,
                TargetDate: null,
                Priority: Priority.Medium,
                ProductOwnerId: null,
                Tags: null,
                Links: null));

        CreateProjectResponse? created = await response.ReadJsonAsync<CreateProjectResponse>();
        Assert.NotNull(created);
        return created.Id;
    }
}
