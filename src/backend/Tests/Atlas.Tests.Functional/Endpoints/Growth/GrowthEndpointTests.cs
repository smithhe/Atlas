using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Growth;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.Growth;

public sealed class GrowthEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GrowthEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EnsureGrowthForTeamMember_WithEmptyJsonBody_CreatesAndReturnsGrowthPlan()
    {
        CreateTeamMemberRequest memberRequest = new($"Growth-{Guid.NewGuid():N}", "Engineer", StatusDot.Green);
        HttpResponseMessage memberResponse = await _client.PostJsonAsync("/team-members", memberRequest);
        CreateTeamMemberResponse? member = await memberResponse.ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(member);

        HttpResponseMessage ensureResponse = await _client.PostEmptyJsonAsync(
            $"/team-members/{member.Id}/growth/ensure");
        Assert.Equal(HttpStatusCode.OK, ensureResponse.StatusCode);

        EnsureGrowthForTeamMemberResponse? ensured = await ensureResponse.ReadJsonAsync<EnsureGrowthForTeamMemberResponse>();
        Assert.NotNull(ensured);
        Assert.NotEqual(Guid.Empty, ensured.GrowthId);

        HttpResponseMessage getResponse = await _client.GetAsync($"/growth/{ensured.GrowthId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        GrowthDto? growth = await getResponse.ReadJsonAsync<GrowthDto>();
        Assert.NotNull(growth);
        Assert.Equal(ensured.GrowthId, growth.Id);
    }
}
