using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.AzureDevOps;

namespace Atlas.Tests.Functional.Endpoints.AzureDevOps;

public sealed class AzureDevOpsEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly AtlasWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AzureDevOpsEndpointTests(AtlasWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListAzureProjects_ReturnsFakeProjects()
    {
        HttpResponseMessage response = await _client.GetAsync("/azure-devops/projects?organization=contoso");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        IReadOnlyList<AzureProjectDto>? projects = await response.ReadJsonAsync<IReadOnlyList<AzureProjectDto>>();
        Assert.NotNull(projects);
        Assert.Equal(_factory.AzureDevOps.Projects.Count, projects.Count);
        Assert.Contains(projects, p => p.Name == "Atlas");
    }

    [Fact]
    public async Task ListAzureTeams_WhenMissingQueryParams_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _client.GetAsync("/azure-devops/teams");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListAzureTeams_ReturnsFakeTeams()
    {
        HttpResponseMessage response = await _client.GetAsync("/azure-devops/teams?organization=contoso&projectId=proj-1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        IReadOnlyList<AzureTeamDto>? teams = await response.ReadJsonAsync<IReadOnlyList<AzureTeamDto>>();
        Assert.NotNull(teams);
        Assert.Equal(_factory.AzureDevOps.Teams.Count, teams.Count);
    }

    [Fact]
    public async Task ListAzureUsers_ReturnsFakeUsers()
    {
        HttpResponseMessage response = await _client.GetAsync(
            "/azure-devops/users?organization=contoso&projectId=proj-1&teamId=team-1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAzureConnection_WhenMissing_ReturnsNotFound()
    {
        using AtlasWebApplicationFactory isolatedFactory = new();
        HttpClient client = isolatedFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/azure-devops/connection");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAndGetAzureConnection_Works()
    {
        HttpResponseMessage updateResponse = await _client.PutJsonAsync(
            "/azure-devops/connection",
            new UpdateAzureConnectionRequest(
                Organization: "contoso",
                Project: "Atlas",
                AreaPath: "Atlas\\Core",
                TeamName: "Core",
                IsEnabled: true,
                ProjectId: "proj-1",
                TeamId: "team-1"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        HttpResponseMessage getResponse = await _client.GetAsync("/azure-devops/connection");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        AzureConnectionDto? connection = await getResponse.ReadJsonAsync<AzureConnectionDto>();
        Assert.NotNull(connection);
        Assert.Equal("contoso", connection.Organization);
        Assert.Equal("proj-1", connection.ProjectId);
    }

    [Fact]
    public async Task GetAzureSyncState_WhenMissing_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync("/azure-devops/sync-state");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListImportedAzureUsers_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/azure-devops/import/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
