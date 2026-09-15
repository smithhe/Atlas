using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{
/// <summary>Growth mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class GrowthService : IGrowthService
{
    private readonly IAtlasApiClient _api;
    private readonly IAppCacheService _cache;
    private readonly KeyedDebounceGate _debounceGate;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private CancellationTokenSource _persistCts;

    private Guid? _persistMemberId;
    private Guid? _persistGoalId;
    private long _routeGeneration;

    public event Action<string>? PersistFailed;

    public GrowthService(IAtlasApiClient api, IAppCacheService cache)
    {
        _api = api;
        _cache = cache;
        _persistCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
        _debounceGate = new KeyedDebounceGate(_lifetimeCts.Token);
    }

    public void AbandonGoalPersists()
    {
        CancelInFlightPersists();
        _persistMemberId = null;
        _persistGoalId = null;
    }

    public string? UpdateGoal(Guid memberId, Guid goalId, Func<GrowthGoal, GrowthGoal> update)
    {
        Growth? growth = _cache.GetGrowth(memberId);
        GrowthGoal? currentGoal = growth?.Goals.FirstOrDefault(g => g.Id == goalId);
        if (growth is null || currentGoal is null)
        {
            return null;
        }

        GrowthGoal baseGoal = CloneGoal(currentGoal);
        baseGoal.LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o");
        GrowthGoal nextGoal = update(baseGoal);

        string? validationError = GrowthUiHelpers.ValidateGoalPersist(nextGoal);
        if (validationError is not null)
        {
            return validationError;
        }

        _cache.UpdateGrowth(EntityClone.ReplaceGoal(growth, goalId, _ => nextGoal));

        if (growth.Id == Guid.Empty)
        {
            return null;
        }

        EnsurePersistScope(memberId, goalId);
        SchedulePersist(
            new KeyedDebounceGate.Key("goal", goalId),
            memberId,
            goalId,
            () =>
            {
                if (!IsActiveForPersist(memberId, goalId))
                {
                    return Task.CompletedTask;
                }

                Growth? g = _cache.GetGrowth(memberId);
                GrowthGoal? goal = GetGoalSnapshot(memberId, goalId);
                return g is null || goal is null || g.Id == Guid.Empty
                    ? Task.CompletedTask
                    : PersistGoalHttpAsync(g.Id, goal, _persistCts.Token);
            },
            "Unable to save goal changes.");
        return null;
    }

    public string? UpdateAction(Guid memberId, Guid goalId, Guid actionId, Action<GrowthGoalAction> patch)
    {
        Growth? growth = _cache.GetGrowth(memberId);
        GrowthGoal? currentGoal = growth?.Goals.FirstOrDefault(g => g.Id == goalId);
        if (growth is null || currentGoal is null)
        {
            return null;
        }

        GrowthGoal baseGoal = CloneGoal(currentGoal);
        baseGoal.LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o");
        GrowthGoal nextGoal = CloneGoal(baseGoal);
        nextGoal.Actions = nextGoal.Actions.Select(a =>
        {
            if (a.Id != actionId)
            {
                return a;
            }

            GrowthGoalAction copy = CloneAction(a);
            patch(copy);
            return copy;
        }).ToList();

        GrowthGoalAction action = nextGoal.Actions.First(a => a.Id == actionId);
        string? validationError = GrowthUiHelpers.ValidateActionPersist(action);
        if (validationError is not null)
        {
            return validationError;
        }

        _cache.UpdateGrowth(EntityClone.ReplaceGoal(growth, goalId, _ => nextGoal));

        if (growth.Id == Guid.Empty)
        {
            return null;
        }

        EnsurePersistScope(memberId, goalId);
        SchedulePersist(
            new KeyedDebounceGate.Key("action", actionId),
            memberId,
            goalId,
            () =>
            {
                if (!IsActiveForPersist(memberId, goalId))
                {
                    return Task.CompletedTask;
                }

                Growth? g = _cache.GetGrowth(memberId);
                GrowthGoalAction? snapshot = GetActionSnapshot(memberId, goalId, actionId);
                return g is null || snapshot is null || g.Id == Guid.Empty
                    ? Task.CompletedTask
                    : PersistActionHttpAsync(g.Id, goalId, snapshot, _persistCts.Token);
            },
            "Unable to save action changes.");
        return null;
    }

    public string? UpdateCheckIn(Guid memberId, Guid goalId, Guid checkInId, Action<GrowthGoalCheckIn> patch)
    {
        Growth? growth = _cache.GetGrowth(memberId);
        GrowthGoal? currentGoal = growth?.Goals.FirstOrDefault(g => g.Id == goalId);
        if (growth is null || currentGoal is null)
        {
            return null;
        }

        GrowthGoal baseGoal = CloneGoal(currentGoal);
        baseGoal.LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o");
        GrowthGoal nextGoal = CloneGoal(baseGoal);
        nextGoal.CheckIns = nextGoal.CheckIns.Select(c =>
        {
            if (c.Id != checkInId)
            {
                return c;
            }

            GrowthGoalCheckIn copy = CloneCheckIn(c);
            patch(copy);
            return copy;
        }).ToList();

        GrowthGoalCheckIn checkIn = nextGoal.CheckIns.First(c => c.Id == checkInId);
        string? validationError = GrowthUiHelpers.ValidateCheckInPersist(checkIn);
        if (validationError is not null)
        {
            return validationError;
        }

        _cache.UpdateGrowth(EntityClone.ReplaceGoal(growth, goalId, _ => nextGoal));

        if (growth.Id == Guid.Empty)
        {
            return null;
        }

        EnsurePersistScope(memberId, goalId);
        SchedulePersist(
            new KeyedDebounceGate.Key("checkin", checkInId),
            memberId,
            goalId,
            () =>
            {
                if (!IsActiveForPersist(memberId, goalId))
                {
                    return Task.CompletedTask;
                }

                Growth? g = _cache.GetGrowth(memberId);
                GrowthGoalCheckIn? snapshot = GetCheckInSnapshot(memberId, goalId, checkInId);
                return g is null || snapshot is null || g.Id == Guid.Empty
                    ? Task.CompletedTask
                    : PersistCheckInHttpAsync(g.Id, goalId, snapshot, _persistCts.Token);
            },
            "Unable to save check-in changes.");
        return null;
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

    public void Dispose()
    {
        _lifetimeCts.Cancel();
        _persistCts.Cancel();
        _persistCts.Dispose();
        _debounceGate.Dispose();
        _lifetimeCts.Dispose();
    }

    private void EnsurePersistScope(Guid memberId, Guid goalId)
    {
        if (_persistMemberId == memberId && _persistGoalId == goalId)
        {
            return;
        }

        CancelInFlightPersists();
        _persistMemberId = memberId;
        _persistGoalId = goalId;
    }

    private void CancelInFlightPersists()
    {
        _routeGeneration++;
        _debounceGate.InvalidateAll();
        _persistCts.Cancel();
        _persistCts.Dispose();
        _persistCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
    }

    private void SchedulePersist(
        KeyedDebounceGate.Key key,
        Guid memberId,
        Guid goalId,
        Func<Task> persist,
        string failureMessage)
    {
        long version = _debounceGate.BumpVersion(key);
        long routeGen = _routeGeneration;
        _debounceGate.DebounceKeyed(
            key,
            version,
            routeGen,
            () => _routeGeneration,
            () => IsActiveForPersist(memberId, goalId),
            work => work(),
            async () =>
            {
                if (!IsActiveForPersist(memberId, goalId) || _persistCts.Token.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await persist();
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    if (!IsActiveForPersist(memberId, goalId) || _persistCts.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    CancelInFlightPersists();
                    // Raise before reload so ShellLayout can alert even if the page
                    // is disposed while RetryGrowthLoadAsync is still in flight.
                    PersistFailed?.Invoke(GrowthUiHelpers.FormatUserError(failureMessage, ex));
                    await _cache.RetryGrowthLoadAsync(memberId);
                }
            });
    }

    private bool IsActiveForPersist(Guid memberId, Guid goalId) =>
        _persistMemberId == memberId
        && _persistGoalId == goalId
        && _cache.GetGrowth(memberId)?.Id != Guid.Empty;

    private GrowthGoal? GetGoalSnapshot(Guid memberId, Guid goalId) =>
        _cache.GetGrowth(memberId)?.Goals.FirstOrDefault(g => g.Id == goalId);

    private GrowthGoalAction? GetActionSnapshot(Guid memberId, Guid goalId, Guid actionId) =>
        GetGoalSnapshot(memberId, goalId)?.Actions.FirstOrDefault(a => a.Id == actionId);

    private GrowthGoalCheckIn? GetCheckInSnapshot(Guid memberId, Guid goalId, Guid checkInId) =>
        GetGoalSnapshot(memberId, goalId)?.CheckIns.FirstOrDefault(c => c.Id == checkInId);

    private Task PersistGoalHttpAsync(Guid growthId, GrowthGoal goal, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsGrowthGoalsUpdateGrowthGoalEndpointAsync(
            growthId,
            goal.Id,
            EntityRequestMappers.ToUpdateGrowthGoalRequest(goal),
            cancellationToken);

    private Task PersistActionHttpAsync(
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

    private Task PersistCheckInHttpAsync(
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

    private async Task OptimisticPatchGrowth(Guid memberId, Func<Growth, Growth> mutator, Func<Task> http) =>
        await OptimisticCache.PatchAsync(
            _cache.GetGrowth(memberId),
            g => EntityClone.Growth(g),
            mutator,
            g => _cache.UpdateGrowth(g),
            http);

    private void PatchGrowth(Guid memberId, Func<Growth, Growth> mutator)
    {
        Growth? current = _cache.GetGrowth(memberId);
        if (current is null)
        {
            return;
        }

        _cache.UpdateGrowth(mutator(current));
    }

    private static GrowthGoal CloneGoal(GrowthGoal g) =>
        EntityClone.Goal(
            g,
            successCriteria: g.SuccessCriteria.ToList(),
            actions: g.Actions.Select(CloneAction).ToList(),
            checkIns: g.CheckIns.Select(CloneCheckIn).ToList());

    private static GrowthGoalAction CloneAction(GrowthGoalAction a) =>
        EntityClone.Action(a, links: a.Links.ToList());

    private static GrowthGoalCheckIn CloneCheckIn(GrowthGoalCheckIn c) => EntityClone.CheckIn(c);
}
}
