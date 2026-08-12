using System.Net;
using System.Net.Http;
using System.Text.Json;
using Atlas.Domain.Enums;
using Atlas.Tests.Functional.Helpers;

namespace Atlas.Tests.Functional.Endpoints.Growth;

public sealed class GrowthValidationEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GrowthValidationEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SetSkillsInProgress_WithEmptySkill_ReturnsBadRequestWithValidationErrors()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);

        HttpResponseMessage response = await _client.PutJsonAsync(
            $"/growth/{growthId}/skills-in-progress",
            new { skillsInProgress = new[] { string.Empty } });

        await AssertValidationBadRequestAsync(response);
    }

    [Fact]
    public async Task AddFeedbackTheme_WithEmptyTitle_ReturnsBadRequestWithValidationErrors()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);

        HttpResponseMessage response = await _client.PostJsonAsync(
            $"/growth/{growthId}/feedback-themes",
            new { title = string.Empty, description = "Clear updates", observedSinceLabel = "Q1" });

        await AssertValidationBadRequestAsync(response);
    }

    [Fact]
    public async Task AddGrowthGoalCheckIn_WithEmptyNote_ReturnsBadRequestWithValidationErrors()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);
        Guid goalId = await GrowthTestHelper.AddGoalAsync(_client, growthId, "Goal");

        HttpResponseMessage response = await _client.PostJsonAsync(
            $"/growth/{growthId}/goals/{goalId}/check-ins",
            new
            {
                date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("O")[..10],
                signal = GrowthGoalCheckInSignal.Positive.ToString(),
                note = string.Empty
            });

        await AssertValidationBadRequestAsync(response);
    }

    [Fact]
    public async Task AddGrowthGoal_WithTargetDateBeforeStartDate_ReturnsBadRequestWithValidationErrors()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);

        HttpResponseMessage response = await _client.PostJsonAsync(
            $"/growth/{growthId}/goals",
            new
            {
                title = "Improve facilitation",
                description = "Lead retros",
                status = GrowthGoalStatus.OnTrack.ToString(),
                startDate = "2026-08-01",
                targetDate = "2026-07-01",
                category = "Leadership",
                priority = Priority.Medium.ToString()
            });

        await AssertValidationBadRequestAsync(response);
    }

    [Fact]
    public async Task AddFeedbackTheme_WithValidClientShapedPayload_ReturnsCreated()
    {
        (_, Guid growthId) = await GrowthTestHelper.EnsureGrowthPlanAsync(_client);

        HttpResponseMessage response = await _client.PostJsonAsync(
            $"/growth/{growthId}/feedback-themes",
            new { title = "Communication", description = "Clear updates", observedSinceLabel = "Q1" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task AssertValidationBadRequestAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" at Atlas.", body, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        Assert.Equal(400, root.GetProperty("statusCode").GetInt32());
        Assert.True(root.TryGetProperty("errors", out JsonElement errors));
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.EnumerateObject().Any());
    }
}
