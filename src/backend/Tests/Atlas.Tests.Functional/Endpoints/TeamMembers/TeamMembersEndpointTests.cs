using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.TeamMembers;

public sealed class TeamMembersEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TeamMembersEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListTeamMembers_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/team-members");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListTeamMembers_ReturnsFullTeamMemberDtos()
    {
        string name = $"List-{Guid.NewGuid():N}";
        CreateTeamMemberResponse? created = await (await _client.PostJsonAsync(
            "/team-members",
            new CreateTeamMemberRequest(name, "Engineer", StatusDot.Green)))
            .ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(created);

        IReadOnlyList<TeamMemberDto>? members = await (await _client.GetAsync("/team-members"))
            .ReadJsonAsync<IReadOnlyList<TeamMemberDto>>();
        Assert.NotNull(members);

        TeamMemberDto listed = Assert.Single(members, m => m.Id == created.Id);
        Assert.Equal(name, listed.Name);
        Assert.NotNull(listed.Profile);
        Assert.NotNull(listed.Signals);
        Assert.NotNull(listed.Notes);
        Assert.NotNull(listed.Risks);
        Assert.NotNull(listed.AzureWorkItems);
    }

    [Fact]
    public async Task CreateGetUpdateDeleteTeamMember_CompletesCrudFlow()
    {
        string name = $"Member-{Guid.NewGuid():N}";

        CreateTeamMemberRequest createRequest = new(name, "Engineer", StatusDot.Green);

        HttpResponseMessage createResponse = await _client.PostJsonAsync("/team-members", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateTeamMemberResponse? created = await createResponse.ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(created);

        HttpResponseMessage getResponse = await _client.GetAsync($"/team-members/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        TeamMemberDto? member = await getResponse.ReadJsonAsync<TeamMemberDto>();
        Assert.NotNull(member);
        Assert.Equal(name, member.Name);

        UpdateTeamMemberRequest updateRequest = new(
            Name: $"{name}-updated",
            Role: "Lead",
            StatusDot: StatusDot.Yellow,
            CurrentFocus: "Testing");

        HttpResponseMessage updateResponse = await _client.PutJsonAsync($"/team-members/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/team-members/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage missingResponse = await _client.GetAsync($"/team-members/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }
}
