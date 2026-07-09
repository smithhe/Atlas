using System.Net.Http;
using Atlas.Api.DTOs.Growth;
using Atlas.Api.DTOs.Growth.Goals;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Integration.Endpoints.Growth;

public sealed class GrowthWorkflowIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly HttpClient _client;

    public GrowthWorkflowIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EnsureGrowthTwice_ReturnsSamePlanAndPersistsGoal()
    {
        CreateTeamMemberResponse? member = await (await _client.PostJsonAsync(
            "/team-members",
            new CreateTeamMemberRequest($"Growth-{Guid.NewGuid():N}", "Engineer", StatusDot.Green)))
            .ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(member);

        EnsureGrowthForTeamMemberResponse? first = await (await _client.PostJsonAsync(
            $"/team-members/{member.Id}/growth/ensure",
            new EnsureGrowthForTeamMemberRequest(member.Id))).ReadJsonAsync<EnsureGrowthForTeamMemberResponse>();
        EnsureGrowthForTeamMemberResponse? second = await (await _client.PostJsonAsync(
            $"/team-members/{member.Id}/growth/ensure",
            new EnsureGrowthForTeamMemberRequest(member.Id))).ReadJsonAsync<EnsureGrowthForTeamMemberResponse>();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.GrowthId, second.GrowthId);

        AddGrowthGoalResponse? goal = await (await _client.PostJsonAsync(
            $"/growth/{first.GrowthId}/goals",
            new AddGrowthGoalRequest(
                first.GrowthId,
                "Mentoring",
                "Grow others",
                GrowthGoalStatus.OnTrack,
                null,
                null,
                "Leadership",
                Priority.Medium))).ReadJsonAsync<AddGrowthGoalResponse>();
        Assert.NotNull(goal);

        GrowthDto? byMember = await (await _client.GetAsync($"/team-members/{member.Id}/growth"))
            .ReadJsonAsync<GrowthDto>();
        Assert.NotNull(byMember);
        Assert.Equal(first.GrowthId, byMember.Id);
        Assert.Single(byMember.Goals);
        Assert.Equal("Mentoring", byMember.Goals[0].Title);
    }
}
