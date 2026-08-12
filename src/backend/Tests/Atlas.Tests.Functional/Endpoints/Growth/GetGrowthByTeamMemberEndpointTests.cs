using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Growth;
using Atlas.Tests.Functional.Helpers;
using FluentAssertions;

namespace Atlas.Tests.Functional.Endpoints.Growth;

public sealed class GetGrowthByTeamMemberEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GetGrowthByTeamMemberEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGrowthByTeamMember_WhenPlanExists_ReturnsOk()
    {
        (Guid memberId, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);

        HttpResponseMessage response = await _client.GetAsync($"/team-members/{memberId}/growth");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GrowthDto? growth = await response.ReadJsonAsync<GrowthDto>();
        growth.Should().NotBeNull();
        growth.Id.Should().Be(growthId);
    }

    [Fact]
    public async Task GetGrowthByTeamMember_WhenMissing_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync($"/team-members/{Guid.NewGuid()}/growth");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
