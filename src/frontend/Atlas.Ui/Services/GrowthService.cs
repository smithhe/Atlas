using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Growth mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class GrowthService
{
    private readonly IAtlasApiClient _api;
    private readonly AppCacheService _cache;

    public GrowthService(IAtlasApiClient api, AppCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<GrowthGoal> AddGoalAsync(
        Guid memberId,
        Guid growthId,
        GrowthGoal draft,
        CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsGrowthGoalsAddGrowthGoalResponse res =
            await _api.AtlasApiEndpointsGrowthGoalsAddGrowthGoalEndpointAsync(
                growthId,
                EntityRequestMappers.ToAddGrowthGoalRequest(draft),
                cancellationToken);

        draft.Id = res.Id ?? Guid.Empty;
        PatchGrowth(memberId, g => EntityClone.Growth(g, goals: new[] { draft }.Concat(g.Goals).ToList()));
        return draft;
    }

    public Task PersistGoalHttpAsync(Guid growthId, GrowthGoal goal, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsUpdateGrowthGoalEndpointAsync(
            growthId,
            goal.Id,
            EntityRequestMappers.ToUpdateGrowthGoalRequest(goal),
            cancellationToken);

    public async Task<GrowthGoalAction> AddActionAsync(
        Guid memberId,
        Guid growthId,
        Guid goalId,
        GrowthGoalAction draft,
        CancellationToken cancellationToken = default)
    {
        GrowthGoal? goal = _cache.GetGrowth(memberId)?.Goals.FirstOrDefault(g => g.Id == goalId);
        AtlasApiDTOsGrowthGoalsActionsAddGrowthGoalActionResponse res =
            await _api.AtlasApiEndpointsGrowthGoalsActionsAddGrowthGoalActionEndpointAsync(
                growthId,
                goalId,
                EntityRequestMappers.ToAddGrowthGoalActionRequest(draft, goal?.Priority),
                cancellationToken);

        draft.Id = res.Id ?? Guid.Empty;
        PatchGrowth(memberId, g => EntityClone.ReplaceGoal(g, goalId, goal =>
        {
            List<GrowthGoalAction> actions = new() { draft };
            actions.AddRange(goal.Actions);
            return EntityClone.Goal(goal, actions: actions);
        }));
        return draft;
    }

    public Task PersistActionHttpAsync(
        Guid growthId,
        Guid goalId,
        GrowthGoalAction action,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsActionsUpdateGrowthGoalActionEndpointAsync(
            growthId,
            goalId,
            action.Id,
            EntityRequestMappers.ToUpdateGrowthGoalActionRequest(action),
            cancellationToken);

    public async Task<GrowthGoalCheckIn> AddCheckInAsync(
        Guid memberId,
        Guid growthId,
        Guid goalId,
        GrowthGoalCheckIn draft,
        CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsGrowthGoalsCheckInsAddGrowthGoalCheckInResponse res =
            await _api.AtlasApiEndpointsGrowthGoalsCheckInsAddGrowthGoalCheckInEndpointAsync(
                growthId,
                goalId,
                EntityRequestMappers.ToAddGrowthGoalCheckInRequest(draft),
                cancellationToken);

        draft.Id = res.Id ?? Guid.Empty;
        PatchGrowth(memberId, g => EntityClone.ReplaceGoal(g, goalId, goal =>
        {
            List<GrowthGoalCheckIn> checkIns = new() { draft };
            checkIns.AddRange(goal.CheckIns);
            return EntityClone.Goal(goal, checkIns: checkIns);
        }));
        return draft;
    }

    public Task PersistCheckInHttpAsync(
        Guid growthId,
        Guid goalId,
        GrowthGoalCheckIn checkIn,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsCheckInsUpdateGrowthGoalCheckInEndpointAsync(
            growthId,
            goalId,
            checkIn.Id,
            EntityRequestMappers.ToUpdateGrowthGoalCheckInRequest(checkIn),
            cancellationToken);

    public async Task SetSkillsInProgressAsync(
        Guid memberId,
        Guid growthId,
        IReadOnlyList<string> skills,
        CancellationToken cancellationToken = default)
    {
        await OptimisticPatchGrowth(
            memberId,
            g => EntityClone.Growth(g, skillsInProgress: skills.ToList()),
            () => _api.AtlasApiEndpointsGrowthSetGrowthSkillsInProgressEndpointAsync(
                growthId,
                EntityRequestMappers.ToSetGrowthSkillsInProgressRequest(skills),
                cancellationToken));
    }

    public async Task<GrowthFeedbackTheme> AddFeedbackThemeAsync(
        Guid memberId,
        Guid growthId,
        GrowthFeedbackTheme draft,
        CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsGrowthFeedbackThemesAddFeedbackThemeResponse res =
            await _api.AtlasApiEndpointsGrowthFeedbackThemesAddFeedbackThemeEndpointAsync(
                growthId,
                EntityRequestMappers.ToAddFeedbackThemeRequest(draft),
                cancellationToken);

        draft.Id = res.Id ?? Guid.Empty;
        PatchGrowth(memberId, g => EntityClone.Growth(g, feedbackThemes: g.FeedbackThemes.Append(draft).ToList()));
        return draft;
    }

    public async Task UpdateFeedbackThemeAsync(
        Guid memberId,
        Guid growthId,
        GrowthFeedbackTheme theme,
        CancellationToken cancellationToken = default)
    {
        await OptimisticPatchGrowth(
            memberId,
            g => EntityClone.Growth(g, feedbackThemes: g.FeedbackThemes.Select(t => t.Id == theme.Id ? theme : t).ToList()),
            () => _api.AtlasApiEndpointsGrowthFeedbackThemesUpdateFeedbackThemeEndpointAsync(
                growthId,
                theme.Id,
                EntityRequestMappers.ToUpdateFeedbackThemeRequest(theme),
                cancellationToken));
    }

    public async Task DeleteFeedbackThemeAsync(Guid memberId, Guid growthId, Guid themeId, CancellationToken cancellationToken = default)
    {
        await _api.AtlasApiEndpointsGrowthFeedbackThemesDeleteFeedbackThemeEndpointAsync(growthId, themeId, cancellationToken);
        PatchGrowth(memberId, g => EntityClone.Growth(g, feedbackThemes: g.FeedbackThemes.Where(t => t.Id != themeId).ToList()));
    }

    public async Task UpdateFocusAreasAsync(Guid memberId, Guid growthId, string? markdown, CancellationToken cancellationToken = default)
    {
        await OptimisticPatchGrowth(
            memberId,
            g => EntityClone.Growth(g, focusAreasMarkdown: markdown ?? ""),
            () => _api.AtlasApiEndpointsGrowthUpdateGrowthFocusAreasEndpointAsync(
                growthId,
                EntityRequestMappers.ToUpdateGrowthFocusAreasRequest(markdown),
                cancellationToken));
    }

    private async Task OptimisticPatchGrowth(Guid memberId, Func<Growth, Growth> mutator, Func<Task> http)
    {
        Growth? previous = _cache.GetGrowth(memberId);
        if (previous is null)
        {
            await http();
            return;
        }

        Growth previousClone = EntityClone.Growth(previous);
        _cache.UpdateGrowth(mutator(previous));
        try
        {
            await http();
        }
        catch
        {
            _cache.UpdateGrowth(previousClone);
            throw;
        }
    }

    private void PatchGrowth(Guid memberId, Func<Growth, Growth> mutator)
    {
        Growth? current = _cache.GetGrowth(memberId);
        if (current is null)
        {
            return;
        }

        _cache.UpdateGrowth(mutator(current));
    }
}
