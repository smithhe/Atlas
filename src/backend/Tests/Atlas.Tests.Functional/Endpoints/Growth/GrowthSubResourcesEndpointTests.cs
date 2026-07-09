using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Growth;
using Atlas.Api.DTOs.Growth.FeedbackThemes;
using Atlas.Api.DTOs.Growth.Goals.Actions;
using Atlas.Api.DTOs.Growth.Goals.CheckIns;
using Atlas.Domain.Enums;
using Atlas.Tests.Functional.Helpers;

namespace Atlas.Tests.Functional.Endpoints.Growth;

public sealed class GrowthSubResourcesEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GrowthSubResourcesEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GrowthSubResources_FullFlow_PersistsGoalsActionsCheckInsThemesAndSkills()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);
        Guid goalId = await GrowthTestHelper.AddGoalAsync(_client, growthId, "Learn testing");

        HttpResponseMessage actionResponse = await _client.PostJsonAsync(
            $"/growth/{growthId}/goals/{goalId}/actions",
            new AddGrowthGoalActionRequest(
                growthId, goalId, "Read docs", GrowthGoalActionState.Planned, null, Priority.Low, null, null));
        AddGrowthGoalActionResponse? action = await actionResponse.ReadJsonAsync<AddGrowthGoalActionResponse>();
        Assert.NotNull(action);

        HttpResponseMessage checkInResponse = await _client.PostJsonAsync(
            $"/growth/{growthId}/goals/{goalId}/check-ins",
            new AddGrowthGoalCheckInRequest(
                growthId, goalId, DateOnly.FromDateTime(DateTime.UtcNow), GrowthGoalCheckInSignal.Positive, "Good progress"));
        AddGrowthGoalCheckInResponse? checkIn = await checkInResponse.ReadJsonAsync<AddGrowthGoalCheckInResponse>();
        Assert.NotNull(checkIn);

        HttpResponseMessage themeResponse = await _client.PostJsonAsync(
            $"/growth/{growthId}/feedback-themes",
            new AddFeedbackThemeRequest(growthId, "Communication", "Clear updates", "Q1"));
        AddFeedbackThemeResponse? theme = await themeResponse.ReadJsonAsync<AddFeedbackThemeResponse>();
        Assert.NotNull(theme);

        HttpResponseMessage focusResponse = await _client.PutJsonAsync(
            $"/growth/{growthId}/focus-areas",
            new UpdateGrowthFocusAreasRequest(growthId, "## Focus\n- Testing"));
        Assert.Equal(HttpStatusCode.NoContent, focusResponse.StatusCode);

        HttpResponseMessage skillsResponse = await _client.PutJsonAsync(
            $"/growth/{growthId}/skills-in-progress",
            new SetGrowthSkillsInProgressRequest(growthId, ["xUnit", "Integration testing"]));
        Assert.Equal(HttpStatusCode.NoContent, skillsResponse.StatusCode);

        GrowthDto? growth = await (await _client.GetAsync($"/growth/{growthId}")).ReadJsonAsync<GrowthDto>();
        Assert.NotNull(growth);
        Assert.Single(growth.Goals);
        Assert.Equal("Learn testing", growth.Goals[0].Title);
        Assert.Single(growth.Goals[0].Actions);
        Assert.Single(growth.Goals[0].CheckIns);
        Assert.Single(growth.FeedbackThemes);
        Assert.Equal(2, growth.SkillsInProgress.Count);
        Assert.Contains("Testing", growth.FocusAreasMarkdown);

        HttpResponseMessage deleteCheckIn = await _client.DeleteAsync(
            $"/growth/{growthId}/goals/{goalId}/check-ins/{checkIn.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCheckIn.StatusCode);

        HttpResponseMessage deleteAction = await _client.DeleteAsync(
            $"/growth/{growthId}/goals/{goalId}/actions/{action.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteAction.StatusCode);

        HttpResponseMessage deleteTheme = await _client.DeleteAsync(
            $"/growth/{growthId}/feedback-themes/{theme.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteTheme.StatusCode);

        HttpResponseMessage deleteGoal = await _client.DeleteAsync($"/growth/{growthId}/goals/{goalId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteGoal.StatusCode);
    }
}
