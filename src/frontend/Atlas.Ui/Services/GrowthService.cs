using Atlas.Ui.Api.Generated;

namespace Atlas.Ui.Services;

/// <summary>Growth mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class GrowthService
{
    private readonly IAtlasApiClient _api;

    public GrowthService(IAtlasApiClient api)
    {
        _api = api;
    }

    public Task<AtlasApiDTOsGrowthGoalsAddGrowthGoalResponse> AddGoalAsync(
        Guid growthId,
        AtlasApiDTOsGrowthGoalsAddGrowthGoalRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsAddGrowthGoalEndpointAsync(growthId, request, cancellationToken);

    public Task UpdateGoalAsync(
        Guid growthId,
        Guid goalId,
        AtlasApiDTOsGrowthGoalsUpdateGrowthGoalRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsUpdateGrowthGoalEndpointAsync(growthId, goalId, request, cancellationToken);

    public Task<AtlasApiDTOsGrowthGoalsActionsAddGrowthGoalActionResponse> AddActionAsync(
        Guid growthId,
        Guid goalId,
        AtlasApiDTOsGrowthGoalsActionsAddGrowthGoalActionRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsActionsAddGrowthGoalActionEndpointAsync(growthId, goalId, request, cancellationToken);

    public Task UpdateActionAsync(
        Guid growthId,
        Guid goalId,
        Guid actionId,
        AtlasApiDTOsGrowthGoalsActionsUpdateGrowthGoalActionRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsActionsUpdateGrowthGoalActionEndpointAsync(growthId, goalId, actionId, request, cancellationToken);

    public Task<AtlasApiDTOsGrowthGoalsCheckInsAddGrowthGoalCheckInResponse> AddCheckInAsync(
        Guid growthId,
        Guid goalId,
        AtlasApiDTOsGrowthGoalsCheckInsAddGrowthGoalCheckInRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsCheckInsAddGrowthGoalCheckInEndpointAsync(growthId, goalId, request, cancellationToken);

    public Task UpdateCheckInAsync(
        Guid growthId,
        Guid goalId,
        Guid checkInId,
        AtlasApiDTOsGrowthGoalsCheckInsUpdateGrowthGoalCheckInRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsCheckInsUpdateGrowthGoalCheckInEndpointAsync(growthId, goalId, checkInId, request, cancellationToken);

    public Task SetSkillsInProgressAsync(
        Guid growthId,
        IReadOnlyList<string> skills,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthSetGrowthSkillsInProgressEndpointAsync(
            growthId,
            new AtlasApiDTOsGrowthSetGrowthSkillsInProgressRequest { SkillsInProgress = skills.ToList() },
            cancellationToken);

    public Task<AtlasApiDTOsGrowthFeedbackThemesAddFeedbackThemeResponse> AddFeedbackThemeAsync(
        Guid growthId,
        AtlasApiDTOsGrowthFeedbackThemesAddFeedbackThemeRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthFeedbackThemesAddFeedbackThemeEndpointAsync(growthId, request, cancellationToken);

    public Task UpdateFeedbackThemeAsync(
        Guid growthId,
        Guid themeId,
        AtlasApiDTOsGrowthFeedbackThemesUpdateFeedbackThemeRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthFeedbackThemesUpdateFeedbackThemeEndpointAsync(growthId, themeId, request, cancellationToken);

    public Task DeleteFeedbackThemeAsync(Guid growthId, Guid themeId, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthFeedbackThemesDeleteFeedbackThemeEndpointAsync(growthId, themeId, cancellationToken);

    public Task UpdateFocusAreasAsync(Guid growthId, string? markdown, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthUpdateGrowthFocusAreasEndpointAsync(
            growthId,
            new AtlasApiDTOsGrowthUpdateGrowthFocusAreasRequest { FocusAreasMarkdown = markdown },
            cancellationToken);
}
