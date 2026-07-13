using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Api.DTOs.ProductOwners;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Domain.Entities;
using Atlas.Persistence;
using Atlas.Tests.Functional.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Tests.Functional.Endpoints.AzureDevOps;

public sealed class AzureDevOpsWriteEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly AtlasWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AzureDevOpsWriteEndpointTests(AtlasWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RunAzureSync_WhenNoConnection_ReturnsOkWithZeroCounts()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        AzureSyncResultDto result = await AzureDevOpsTestHelper.RunSyncAsync(client);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ItemsUpserted);
        Assert.Contains("not configured", result.Error);
    }

    [Fact]
    public async Task RunAzureSync_WhenConnectionConfigured_UpsertsWorkItemsAndReturnsCounts()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();
        await AzureDevOpsTestHelper.ConfigureConnectionAsync(client);

        AzureSyncResultDto result = await AzureDevOpsTestHelper.RunSyncAsync(client);

        Assert.True(result.Succeeded);
        Assert.True(result.ItemsUpserted > 0);
        Assert.Equal(isolated.AzureDevOps.WorkItemIds.Count, result.ItemsFetched);

        HttpResponseMessage syncState = await client.GetAsync("/azure-devops/sync-state");
        Assert.Equal(HttpStatusCode.OK, syncState.StatusCode);
        AzureSyncStateDto? state = await syncState.ReadJsonAsync<AzureSyncStateDto>();
        Assert.NotNull(state);
        Assert.Equal("Succeeded", state.LastRunStatus);
    }

    [Fact]
    public async Task ListTeamAreaPaths_ReturnsFakePaths()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/azure-devops/team-area-paths?organization=contoso&projectId=proj-1&teamName=Core");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AzureTeamAreaPathsDto? paths = await response.ReadJsonAsync<AzureTeamAreaPathsDto>();
        Assert.NotNull(paths);
        Assert.Equal(_factory.AzureDevOps.TeamAreaPaths.DefaultValue, paths.DefaultValue);
        Assert.Equal(_factory.AzureDevOps.TeamAreaPaths.Values.Count, paths.Values.Count);
    }

    [Fact]
    public async Task ListTeamAreaPaths_WhenMissingParams_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.GetAsync("/azure-devops/team-area-paths");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ImportAzureTeam_CreatesTeamMembersAndMappings()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        HttpResponseMessage response = await client.PostJsonAsync(
            "/azure-devops/team/import",
            new ImportAzureTeamRequest(
            [
                new AzureUserSelectionDto("Ada Lovelace", "ada@example.com", "user-1")
            ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ImportAzureTeamResultDto? result = await response.ReadJsonAsync<ImportAzureTeamResultDto>();
        Assert.NotNull(result);
        Assert.Equal(1, result.UsersAdded);
        Assert.Equal(1, result.TeamMembersCreated);
        Assert.Equal(1, result.MappingsCreated);

        HttpResponseMessage members = await client.GetAsync("/team-members");
        IReadOnlyList<TeamMemberListItemDto>? list = await members.ReadJsonAsync<IReadOnlyList<TeamMemberListItemDto>>();
        Assert.NotNull(list);
        Assert.Contains(list, m => m.Name == "Ada Lovelace");
    }

    [Fact]
    public async Task ImportAzureTeam_WhenAlreadyImported_IsIdempotent()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();
        var request = new ImportAzureTeamRequest(
        [
            new AzureUserSelectionDto("Grace Hopper", "grace@example.com", "user-2")
        ]);

        ImportAzureTeamResultDto? first = await (await client.PostJsonAsync("/azure-devops/team/import", request))
            .ReadJsonAsync<ImportAzureTeamResultDto>();
        ImportAzureTeamResultDto? second = await (await client.PostJsonAsync("/azure-devops/team/import", request))
            .ReadJsonAsync<ImportAzureTeamResultDto>();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(1, first.TeamMembersCreated);
        Assert.Equal(0, second.TeamMembersCreated);
        Assert.Equal(1, second.UsersUpdated);
    }

    [Fact]
    public async Task ImportAzureProductOwners_CreatesProductOwners()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        HttpResponseMessage response = await client.PostJsonAsync(
            "/azure-devops/product-owners/import",
            new ImportAzureProductOwnersRequest(
            [
                new AzureUserSelectionDto("Product Owner One", "po@example.com", null)
            ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ImportAzureProductOwnersResultDto? result = await response.ReadJsonAsync<ImportAzureProductOwnersResultDto>();
        Assert.NotNull(result);
        Assert.Equal(1, result.ProductOwnersCreated);
        Assert.NotNull(result.ReusedProductOwnerNames);
        Assert.Empty(result.ReusedProductOwnerNames);

        HttpResponseMessage owners = await client.GetAsync("/product-owners");
        IReadOnlyList<ProductOwnerListItemDto>? list = await owners.ReadJsonAsync<IReadOnlyList<ProductOwnerListItemDto>>();
        Assert.NotNull(list);
        Assert.Contains(list, o => o.Name == "Product Owner One");
    }

    [Fact]
    public async Task ImportAzureProductOwners_WhenDisplayNameExists_ReportsReusedNames()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        await client.PostJsonAsync(
            "/azure-devops/product-owners/import",
            new ImportAzureProductOwnersRequest(
            [
                new AzureUserSelectionDto("Shared Owner", "po1@example.com", null)
            ]));

        ImportAzureProductOwnersResultDto? result = await (await client.PostJsonAsync(
            "/azure-devops/product-owners/import",
            new ImportAzureProductOwnersRequest(
            [
                new AzureUserSelectionDto("Shared Owner", "po2@example.com", null)
            ])))
            .ReadJsonAsync<ImportAzureProductOwnersResultDto>();

        Assert.NotNull(result);
        Assert.Equal(0, result.ProductOwnersCreated);
        Assert.Equal(1, result.MappingsCreated);
        ReusedProductOwnerNameDto reused = Assert.Single(result.ReusedProductOwnerNames);
        Assert.Equal("Shared Owner", reused.DisplayName);
        Assert.Equal("po2@example.com", reused.AzureUniqueName);
    }

    [Fact]
    public async Task ImportAzureProductOwners_WhenTeamMemberExists_Skips()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        await client.PostJsonAsync(
            "/azure-devops/team/import",
            new ImportAzureTeamRequest([new AzureUserSelectionDto("Shared", "shared@example.com", null)]));

        ImportAzureProductOwnersResultDto? result = await (await client.PostJsonAsync(
            "/azure-devops/product-owners/import",
            new ImportAzureProductOwnersRequest([new AzureUserSelectionDto("Shared", "shared@example.com", null)])))
            .ReadJsonAsync<ImportAzureProductOwnersResultDto>();

        Assert.NotNull(result);
        Assert.Equal(0, result.ProductOwnersCreated);
        Assert.Equal(0, result.MappingsCreated);
        Assert.NotNull(result.ReusedProductOwnerNames);
        Assert.Empty(result.ReusedProductOwnerNames);
    }

    [Fact]
    public async Task SyncImportLinkWorkflow_ListsUnlinkedThenExcludesAfterLink()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();
        await AzureDevOpsTestHelper.ConfigureConnectionAsync(client);
        await AzureDevOpsTestHelper.RunSyncAsync(client);

        HttpResponseMessage importList = await client.GetAsync("/azure-devops/import/work-items");
        Assert.Equal(HttpStatusCode.OK, importList.StatusCode);
        IReadOnlyList<AzureImportWorkItemDto>? unlinked = await importList.ReadJsonAsync<IReadOnlyList<AzureImportWorkItemDto>>();
        Assert.NotNull(unlinked);
        Assert.NotEmpty(unlinked);

        Guid projectId = await AzureDevOpsTestHelper.CreateProjectAsync(client);
        Guid workItemId = unlinked[0].Id;

        HttpResponseMessage linkResponse = await client.PostJsonAsync(
            "/azure-devops/import/link",
            new LinkAzureWorkItemsRequest([workItemId], projectId, null));
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);
        int linked = await linkResponse.ReadJsonAsync<int>();
        Assert.Equal(1, linked);

        IReadOnlyList<AzureImportWorkItemDto>? afterLink = await (await client.GetAsync("/azure-devops/import/work-items"))
            .ReadJsonAsync<IReadOnlyList<AzureImportWorkItemDto>>();
        Assert.NotNull(afterLink);
        Assert.DoesNotContain(afterLink, x => x.Id == workItemId);
    }

    [Fact]
    public async Task LinkAzureWorkItems_WithTeamMemberId_OverridesMapping()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();
        await AzureDevOpsTestHelper.ConfigureConnectionAsync(client);
        await AzureDevOpsTestHelper.RunSyncAsync(client);

        // Member A + Azure user mapping for the fake assignee (ada@example.com).
        ImportAzureTeamResultDto? import = await (await client.PostJsonAsync(
            "/azure-devops/team/import",
            new ImportAzureTeamRequest(
            [
                new AzureUserSelectionDto("Ada Lovelace", "ada@example.com", "user-1")
            ])))
            .ReadJsonAsync<ImportAzureTeamResultDto>();
        Assert.NotNull(import);
        Assert.Equal(1, import.TeamMembersCreated);

        IReadOnlyList<TeamMemberListItemDto>? members = await (await client.GetAsync("/team-members"))
            .ReadJsonAsync<IReadOnlyList<TeamMemberListItemDto>>();
        Assert.NotNull(members);
        TeamMemberListItemDto memberA = Assert.Single(members, m => m.Name == "Ada Lovelace");

        // Member B is the explicit override target (no mapping to the assignee).
        CreateTeamMemberResponse? memberB = await (await client.PostJsonAsync(
            "/team-members",
            new CreateTeamMemberRequest($"Override-{Guid.NewGuid():N}", "Engineer", Domain.Enums.StatusDot.Green)))
            .ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(memberB);

        IReadOnlyList<AzureImportWorkItemDto>? unlinked = await (await client.GetAsync("/azure-devops/import/work-items"))
            .ReadJsonAsync<IReadOnlyList<AzureImportWorkItemDto>>();
        Assert.NotNull(unlinked);
        Assert.Contains(unlinked, x => x.SuggestedTeamMemberId == memberA.Id);
        AzureImportWorkItemDto mappedItem = unlinked.First(x => x.SuggestedTeamMemberId == memberA.Id);

        Guid projectId = await AzureDevOpsTestHelper.CreateProjectAsync(client);
        int linked = await (await client.PostJsonAsync(
            "/azure-devops/import/link",
            new LinkAzureWorkItemsRequest([mappedItem.Id], projectId, memberB.Id)))
            .ReadJsonAsync<int>();

        Assert.Equal(1, linked);

        using (IServiceScope scope = isolated.Services.CreateScope())
        {
            AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
            AzureWorkItemLink? link = await db.AzureWorkItemLinks
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.AzureWorkItemId == mappedItem.Id);
            Assert.NotNull(link);
            Assert.Equal(memberB.Id, link.TeamMemberId);
            Assert.NotEqual(memberA.Id, link.TeamMemberId);
        }

        string adoId = mappedItem.WorkItemId.ToString();
        TeamMemberDto? detailA = await (await client.GetAsync($"/team-members/{memberA.Id}"))
            .ReadJsonAsync<TeamMemberDto>();
        TeamMemberDto? detailB = await (await client.GetAsync($"/team-members/{memberB.Id}"))
            .ReadJsonAsync<TeamMemberDto>();
        Assert.NotNull(detailA);
        Assert.NotNull(detailB);
        Assert.DoesNotContain(detailA.AzureWorkItems, wi => wi.Id == adoId);
        Assert.Contains(detailB.AzureWorkItems, wi => wi.Id == adoId);
    }
}
