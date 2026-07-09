using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Projects;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.Projects;

public sealed class ProjectTeamMembersEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProjectTeamMembersEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SetProjectTeamMembers_LinksMembersToProject()
    {
        CreateProjectResponse? project = await (await _client.PostJsonAsync("/projects", new CreateProjectRequest(
            Name: $"Proj-{Guid.NewGuid():N}",
            Summary: "Summary",
            Description: null,
            Status: ProjectStatus.Active,
            Health: HealthSignal.Green,
            TargetDate: null,
            Priority: Priority.Medium,
            ProductOwnerId: null,
            Tags: null,
            Links: null))).ReadJsonAsync<CreateProjectResponse>();
        Assert.NotNull(project);

        CreateTeamMemberResponse? member = await (await _client.PostJsonAsync(
            "/team-members",
            new CreateTeamMemberRequest($"Member-{Guid.NewGuid():N}", "Engineer", StatusDot.Green)))
            .ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(member);

        HttpResponseMessage setMembers = await _client.PutJsonAsync(
            $"/projects/{project.Id}/team-members",
            new SetProjectTeamMembersRequest([member.Id]));
        Assert.Equal(HttpStatusCode.NoContent, setMembers.StatusCode);

        ProjectDto? loaded = await (await _client.GetAsync($"/projects/{project.Id}")).ReadJsonAsync<ProjectDto>();
        Assert.NotNull(loaded);
        Assert.Contains(member.Id, loaded.TeamMemberIds);
    }
}
