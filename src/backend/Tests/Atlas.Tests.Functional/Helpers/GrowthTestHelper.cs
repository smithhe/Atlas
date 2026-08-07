using System.Net.Http;
using Atlas.Api.DTOs.Growth;
using Atlas.Api.DTOs.Growth.Goals;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Helpers;

internal static class GrowthTestHelper
{
    public static async Task<(Guid MemberId, Guid GrowthId)> EnsureGrowthPlanAsync(HttpClient client)
    {
        string name = $"Growth-{Guid.NewGuid():N}";
        HttpResponseMessage memberResponse = await client.PostJsonAsync(
            "/team-members",
            new CreateTeamMemberRequest(name, "Engineer", StatusDot.Green));
        CreateTeamMemberResponse? member = await memberResponse.ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(member);

        HttpResponseMessage ensureResponse = await client.PostEmptyJsonAsync(
            $"/team-members/{member.Id}/growth/ensure");
        EnsureGrowthForTeamMemberResponse? ensured = await ensureResponse.ReadJsonAsync<EnsureGrowthForTeamMemberResponse>();
        Assert.NotNull(ensured);

        return (member.Id, ensured.GrowthId);
    }

    public static async Task<Guid> AddGoalAsync(HttpClient client, Guid growthId, string title)
    {
        HttpResponseMessage response = await client.PostJsonAsync(
            $"/growth/{growthId}/goals",
            new AddGrowthGoalRequest(
                growthId,
                title,
                "Description",
                GrowthGoalStatus.OnTrack,
                null,
                null,
                "Engineering",
                Priority.Medium));

        AddGrowthGoalResponse? created = await response.ReadJsonAsync<AddGrowthGoalResponse>();
        Assert.NotNull(created);
        return created.Id;
    }
}
