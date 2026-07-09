using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Projects;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.Projects;

public sealed class ProjectsEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProjectsEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListProjects_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateGetUpdateDeleteProject_CompletesCrudFlow()
    {
        string name = $"Project-{Guid.NewGuid():N}";

        CreateProjectRequest createRequest = new(
            Name: name,
            Summary: "Summary",
            Description: "Description",
            Status: ProjectStatus.Active,
            Health: HealthSignal.Green,
            TargetDate: null,
            Priority: Priority.Medium,
            ProductOwnerId: null,
            Tags: ["atlas"],
            Links: [new ProjectLinkDto("Docs", "https://example.com/docs")]);

        HttpResponseMessage createResponse = await _client.PostJsonAsync("/projects", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateProjectResponse? created = await createResponse.ReadJsonAsync<CreateProjectResponse>();
        Assert.NotNull(created);

        HttpResponseMessage getResponse = await _client.GetAsync($"/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        ProjectDto? project = await getResponse.ReadJsonAsync<ProjectDto>();
        Assert.NotNull(project);
        Assert.Equal(name, project.Name);

        UpdateProjectRequest updateRequest = new(
            Name: $"{name}-updated",
            Summary: "Updated summary",
            Description: "Updated description",
            Status: ProjectStatus.Paused,
            Health: HealthSignal.Yellow,
            TargetDate: null,
            Priority: Priority.High,
            ProductOwnerId: null,
            Tags: ["updated"],
            Links: null);

        HttpResponseMessage updateResponse = await _client.PutJsonAsync($"/projects/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage missingResponse = await _client.GetAsync($"/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_ReplacesTagsAndLinks()
    {
        CreateProjectResponse? created = await (await _client.PostJsonAsync(
            "/projects",
            new CreateProjectRequest(
                Name: $"Tags-{Guid.NewGuid():N}",
                Summary: "Summary",
                Description: null,
                Status: ProjectStatus.Active,
                Health: HealthSignal.Green,
                TargetDate: null,
                Priority: Priority.Medium,
                ProductOwnerId: null,
                Tags: ["alpha", "beta"],
                Links: [new ProjectLinkDto("Docs", "https://example.com/docs")])))
            .ReadJsonAsync<CreateProjectResponse>();
        Assert.NotNull(created);

        HttpResponseMessage update = await _client.PutJsonAsync(
            $"/projects/{created.Id}",
            new UpdateProjectRequest(
                Name: "Updated",
                Summary: "Summary",
                Description: null,
                Status: ProjectStatus.Active,
                Health: HealthSignal.Green,
                TargetDate: null,
                Priority: Priority.Medium,
                ProductOwnerId: null,
                Tags: ["beta", "gamma"],
                Links: [new ProjectLinkDto("Wiki", "https://example.com/wiki")]));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        ProjectDto? project = await (await _client.GetAsync($"/projects/{created.Id}")).ReadJsonAsync<ProjectDto>();
        Assert.NotNull(project);
        Assert.Equal(2, project.Tags.Count);
        Assert.Contains(project.Tags, t => t.Value == "beta");
        Assert.Contains(project.Tags, t => t.Value == "gamma");
        Assert.DoesNotContain(project.Tags, t => t.Value == "alpha");
        Assert.Single(project.Links);
        Assert.Equal("Wiki", project.Links[0].Label);
    }
}
