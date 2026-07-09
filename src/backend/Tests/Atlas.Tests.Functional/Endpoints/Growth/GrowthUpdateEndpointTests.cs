using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Growth;
using Atlas.Api.DTOs.Growth.FeedbackThemes;
using Atlas.Api.DTOs.Growth.Goals;
using Atlas.Api.DTOs.Growth.Goals.Actions;
using Atlas.Api.DTOs.Growth.Goals.CheckIns;
using Atlas.Domain.Enums;
using Atlas.Tests.Functional.Helpers;

namespace Atlas.Tests.Functional.Endpoints.Growth;

public sealed class GrowthUpdateEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GrowthUpdateEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateGrowthGoal_ChangesTitleAndStatus()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);
        Guid goalId = await GrowthTestHelper.AddGoalAsync(_client, growthId, "Original");

        HttpResponseMessage update = await _client.PutJsonAsync(
            $"/growth/{growthId}/goals/{goalId}",
            new UpdateGrowthGoalRequest(
                growthId,
                goalId,
                "Updated goal",
                "Updated description",
                GrowthGoalStatus.NeedsAttention,
                null,
                null,
                "Engineering",
                Priority.High,
                40,
                "Summary",
                ["Ship tests"]));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        GrowthDto? growth = await (await _client.GetAsync($"/growth/{growthId}")).ReadJsonAsync<GrowthDto>();
        Assert.NotNull(growth);
        Assert.Equal("Updated goal", growth.Goals[0].Title);
        Assert.Equal(GrowthGoalStatus.NeedsAttention, growth.Goals[0].Status);
    }

    [Fact]
    public async Task UpdateGrowthGoalAction_ChangesTitle()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);
        Guid goalId = await GrowthTestHelper.AddGoalAsync(_client, growthId, "Goal");

        AddGrowthGoalActionResponse? action = await (await _client.PostJsonAsync(
            $"/growth/{growthId}/goals/{goalId}/actions",
            new AddGrowthGoalActionRequest(
                growthId, goalId, "Draft action", GrowthGoalActionState.Planned, null, Priority.Low, null, null)))
            .ReadJsonAsync<AddGrowthGoalActionResponse>();
        Assert.NotNull(action);

        HttpResponseMessage update = await _client.PutJsonAsync(
            $"/growth/{growthId}/goals/{goalId}/actions/{action.Id}",
            new UpdateGrowthGoalActionRequest(
                growthId, goalId, action.Id, "Finished action", GrowthGoalActionState.Complete, null, Priority.Medium, "notes", null));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        GrowthDto? growth = await (await _client.GetAsync($"/growth/{growthId}")).ReadJsonAsync<GrowthDto>();
        Assert.NotNull(growth);
        Assert.Equal("Finished action", growth.Goals[0].Actions[0].Title);
        Assert.Equal(GrowthGoalActionState.Complete, growth.Goals[0].Actions[0].State);
    }

    [Fact]
    public async Task UpdateGrowthGoalCheckIn_ChangesSignal()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);
        Guid goalId = await GrowthTestHelper.AddGoalAsync(_client, growthId, "Goal");

        AddGrowthGoalCheckInResponse? checkIn = await (await _client.PostJsonAsync(
            $"/growth/{growthId}/goals/{goalId}/check-ins",
            new AddGrowthGoalCheckInRequest(
                growthId, goalId, DateOnly.FromDateTime(DateTime.UtcNow), GrowthGoalCheckInSignal.Mixed, "ok")))
            .ReadJsonAsync<AddGrowthGoalCheckInResponse>();
        Assert.NotNull(checkIn);

        HttpResponseMessage update = await _client.PutJsonAsync(
            $"/growth/{growthId}/goals/{goalId}/check-ins/{checkIn.Id}",
            new UpdateGrowthGoalCheckInRequest(
                growthId, goalId, checkIn.Id, DateOnly.FromDateTime(DateTime.UtcNow), GrowthGoalCheckInSignal.Positive, "great"));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        GrowthDto? growth = await (await _client.GetAsync($"/growth/{growthId}")).ReadJsonAsync<GrowthDto>();
        Assert.NotNull(growth);
        Assert.Equal(GrowthGoalCheckInSignal.Positive, growth.Goals[0].CheckIns[0].Signal);
        Assert.Equal("great", growth.Goals[0].CheckIns[0].Note);
    }

    [Fact]
    public async Task UpdateFeedbackTheme_ChangesDescription()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);

        AddFeedbackThemeResponse? theme = await (await _client.PostJsonAsync(
            $"/growth/{growthId}/feedback-themes",
            new AddFeedbackThemeRequest(growthId, "Theme", "Old description", "Q1")))
            .ReadJsonAsync<AddFeedbackThemeResponse>();
        Assert.NotNull(theme);

        HttpResponseMessage update = await _client.PutJsonAsync(
            $"/growth/{growthId}/feedback-themes/{theme.Id}",
            new UpdateFeedbackThemeRequest(growthId, theme.Id, "Theme", "New description", "Q2"));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        GrowthDto? growth = await (await _client.GetAsync($"/growth/{growthId}")).ReadJsonAsync<GrowthDto>();
        Assert.NotNull(growth);
        Assert.Equal("New description", growth.FeedbackThemes[0].Description);
        Assert.Equal("Q2", growth.FeedbackThemes[0].ObservedSinceLabel);
    }

    [Fact]
    public async Task SetSkillsInProgress_DeduplicatesCaseInsensitive()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);

        HttpResponseMessage response = await _client.PutJsonAsync(
            $"/growth/{growthId}/skills-in-progress",
            new SetGrowthSkillsInProgressRequest(growthId, ["xUnit", "XUNIT", "EF Core"]));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        GrowthDto? growth = await (await _client.GetAsync($"/growth/{growthId}")).ReadJsonAsync<GrowthDto>();
        Assert.NotNull(growth);
        Assert.Equal(2, growth.SkillsInProgress.Count);
        Assert.Equal("xUnit", growth.SkillsInProgress[0]);
        Assert.Equal("EF Core", growth.SkillsInProgress[1]);
    }
}
