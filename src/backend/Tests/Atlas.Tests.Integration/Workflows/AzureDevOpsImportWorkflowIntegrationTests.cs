using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Api.DTOs.Projects;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Integration.Workflows;

// Overlaps AzureDevOpsWriteEndpointTests sync/import/link coverage; kept as an end-to-end workflow proof.
// ADO HTTP helpers stay local (Functional.Helpers is not shared with Integration).
public sealed class AzureDevOpsImportWorkflowIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    [Fact]
    public async Task ConnectionSyncImportLink_ResolvesSuggestedTeamMemberAndExcludesLinkedItems()
    {
        using AtlasIntegrationApplicationFactory factory = new();
        HttpClient client = factory.CreateClient();

        await ConfigureConnectionAsync(client);
        AzureSyncResultDto sync = await RunSyncAsync(client);
        Assert.True(sync.Succeeded);
        Assert.True(sync.ItemsUpserted > 0);

        ImportAzureTeamResultDto? import = await (await client.PostJsonAsync(
            "/azure-devops/team/import",
            new ImportAzureTeamRequest(
            [
                new AzureUserSelectionDto("Ada Lovelace", "ada@example.com", "user-1")
            ])))
            .ReadJsonAsync<ImportAzureTeamResultDto>();
        Assert.NotNull(import);
        Assert.Equal(1, import.TeamMembersCreated);

        IReadOnlyList<TeamMemberDto>? members = await (await client.GetAsync("/team-members"))
            .ReadJsonAsync<IReadOnlyList<TeamMemberDto>>();
        Assert.NotNull(members);
        Assert.Contains(members, m => m.Name == "Ada Lovelace");
        TeamMemberDto ada = members.First(m => m.Name == "Ada Lovelace");

        IReadOnlyList<AzureImportWorkItemDto>? unlinked = await (await client.GetAsync("/azure-devops/import/work-items"))
            .ReadJsonAsync<IReadOnlyList<AzureImportWorkItemDto>>();
        Assert.NotNull(unlinked);
        Assert.NotEmpty(unlinked);
        Assert.Contains(unlinked, x => x.SuggestedTeamMemberId == ada.Id);

        Guid projectId = await CreateProjectAsync(client);
        AzureImportWorkItemDto adaItem = unlinked.First(x => x.SuggestedTeamMemberId == ada.Id);

        int linked = await (await client.PostJsonAsync(
            "/azure-devops/import/link",
            new LinkAzureWorkItemsRequest([adaItem.Id], projectId, null)))
            .ReadJsonAsync<int>();
        Assert.Equal(1, linked);

        IReadOnlyList<AzureImportWorkItemDto>? remaining = await (await client.GetAsync("/azure-devops/import/work-items"))
            .ReadJsonAsync<IReadOnlyList<AzureImportWorkItemDto>>();
        Assert.NotNull(remaining);
        Assert.DoesNotContain(remaining, x => x.Id == adaItem.Id);

        TeamMemberDto? memberDetail = await (await client.GetAsync($"/team-members/{ada.Id}"))
            .ReadJsonAsync<TeamMemberDto>();
        Assert.NotNull(memberDetail);
        Assert.Contains(memberDetail.AzureWorkItems, wi => wi.Id == adaItem.WorkItemId.ToString());
    }

    private static async Task ConfigureConnectionAsync(HttpClient client)
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

    private static async Task<AzureSyncResultDto> RunSyncAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsync("/azure-devops/sync", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AzureSyncResultDto? result = await response.ReadJsonAsync<AzureSyncResultDto>();
        Assert.NotNull(result);
        return result;
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client)
    {
        CreateProjectResponse? created = await (await client.PostJsonAsync(
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
                Links: null)))
            .ReadJsonAsync<CreateProjectResponse>();
        Assert.NotNull(created);
        return created.Id;
    }
}
