using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class GrowthGoalDetail : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] SelectionState Selection { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] GrowthService GrowthService { get; set; } = default!;

    const string DefaultCheckInNote = "New check-in";

    readonly record struct PersistIdentity(long RouteGeneration, Guid MemberId, Guid GrowthId, Guid GoalId, Guid? EntityId);

    [Parameter] public string? MemberId { get; set; }
    [Parameter] public string? GoalId { get; set; }

    Guid? _selectedActionId;
    Guid? _selectedCheckInId;
    string? _routeMemberId;
    string? _routeGoalId;
    Guid _loadedGrowthMemberId;
    string _actionFilter = "All";
    string _actionSort = "DueDate";
    bool _editTimeframe;
    bool _editStatus;
    bool _editCategory;
    bool _editPriority;
    bool _editSummary;
    bool _editSuccessCriteria;
    GrowthLoadStatus _loadStatus;
    string? _loadError;
    bool _retrying;
    bool _disposed;
    bool _routeInitializing;
    long _routeGeneration;
    string _goalValidationError = "";
    string _actionValidationError = "";
    string _checkInValidationError = "";
    readonly CancellationTokenSource _lifetimeCts = new();
    CancellationTokenSource? _goalDebounce;
    readonly Dictionary<Guid, CancellationTokenSource> _actionDebounce = new();
    readonly Dictionary<Guid, CancellationTokenSource> _checkInDebounce = new();
    long _goalPersistVersion;
    readonly Dictionary<Guid, long> _actionPersistVersion = new();
    readonly Dictionary<Guid, long> _checkInPersistVersion = new();
    readonly SemaphoreSlim _goalPersistGate = new(1, 1);
    readonly Dictionary<Guid, SemaphoreSlim> _actionPersistGates = new();
    readonly Dictionary<Guid, SemaphoreSlim> _checkInPersistGates = new();
    readonly object _persistGateLock = new();

    void BackToGrowth() => Nav.NavigateTo($"/team/{MemberId}/growth");

    TeamMember? Member =>
        Guid.TryParse(MemberId, out Guid id) ? Cache.Team.FirstOrDefault(m => m.Id == id) : null;

    Growth? Growth =>
        Guid.TryParse(MemberId, out Guid id) ? Cache.GetGrowth(id) : null;

    GrowthGoal? Goal
    {
        get
        {
            if (Growth is null || !Guid.TryParse(GoalId, out Guid gid)) return null;
            return Growth.Goals.FirstOrDefault(g => g.Id == gid);
        }
    }

    GrowthGoalAction? SelectedAction =>
        _selectedActionId is { } id ? Goal?.Actions.FirstOrDefault(a => a.Id == id) : null;

    GrowthGoalCheckIn? SelectedCheckIn =>
        _selectedCheckInId is { } id ? Goal?.CheckIns.FirstOrDefault(c => c.Id == id) : null;

    int ProgressTotal => Goal?.Actions.Count ?? 0;
    int ProgressDone => Goal?.Actions.Count(a => a.State == GrowthGoalActionState.Complete) ?? 0;
    int ProgressPercent => ProgressTotal == 0 ? 0 : (int)Math.Round(100.0 * ProgressDone / ProgressTotal);

    List<GrowthGoalAction> VisibleActions
    {
        get
        {
            IReadOnlyList<GrowthGoalAction> actions = Goal?.Actions ?? Array.Empty<GrowthGoalAction>();
            IEnumerable<GrowthGoalAction> filtered = _actionFilter == "All"
                ? actions
                : actions.Where(a => a.State.ToString() == _actionFilter);
            return filtered
                .OrderBy(a =>
                {
                    if (_actionSort == "State")
                        return a.State switch
                        {
                            GrowthGoalActionState.InProgress => 0,
                            GrowthGoalActionState.Planned => 1,
                            _ => 2
                        };
                    return 0;
                })
                .ThenBy(a => a.DueDateIso ?? "9999-12-31")
                .ThenBy(a => a.Title)
                .ToList();
        }
    }

    protected override void OnInitialized()
    {
        Cache.Changed += OnChanged;
        _ = Cache.EnsureHydratedAsync();
    }

    protected override void OnParametersSet()
    {
        bool routeChanged = !string.Equals(_routeMemberId, MemberId, StringComparison.Ordinal)
                            || !string.Equals(_routeGoalId, GoalId, StringComparison.Ordinal);
        _routeMemberId = MemberId;
        _routeGoalId = GoalId;

        if (routeChanged)
            InvalidateRouteState();

        if (!string.IsNullOrEmpty(MemberId) && !Guid.TryParse(MemberId, out _))
        {
            Nav.NavigateTo("/team", replace: true);
            return;
        }

        if (Guid.TryParse(MemberId, out Guid id))
        {
            Selection.SelectTeamMember(id);
            _loadStatus = Cache.GetGrowthLoadStatus(id);
            _loadError = Cache.GetGrowthLoadError(id);

            if (id != _loadedGrowthMemberId)
            {
                _loadedGrowthMemberId = id;
                _routeInitializing = true;
                _ = Cache.EnsureGrowthLoadedAsync(id);
            }

            if (Cache.TeamReady && Member is null)
                Nav.NavigateTo("/team", replace: true);

            UpdateRouteInitializingFromLoadStatus();
        }
    }

    void UpdateRouteInitializingFromLoadStatus()
    {
        if (_loadStatus is GrowthLoadStatus.Succeeded or GrowthLoadStatus.Failed)
            _routeInitializing = false;
    }

    void InvalidatePersistWork()
    {
        CancelAllDebounces();
        _goalPersistVersion++;
        foreach (Guid key in _actionPersistVersion.Keys.ToList())
            _actionPersistVersion[key]++;
        foreach (Guid key in _checkInPersistVersion.Keys.ToList())
            _checkInPersistVersion[key]++;
    }

    void InvalidateRouteState()
    {
        CancelAllDebounces();
        _routeGeneration++;
        _goalPersistVersion++;
        foreach (Guid key in _actionPersistVersion.Keys.ToList())
            _actionPersistVersion[key]++;
        foreach (Guid key in _checkInPersistVersion.Keys.ToList())
            _checkInPersistVersion[key]++;
        _goalValidationError = "";
        _actionValidationError = "";
        _checkInValidationError = "";
        _retrying = false;
        _selectedActionId = null;
        _selectedCheckInId = null;
        _editTimeframe = false;
        _editStatus = false;
        _editCategory = false;
        _editPriority = false;
        _editSummary = false;
        _editSuccessCriteria = false;
        _routeInitializing = true;
    }

    void CancelAllDebounces()
    {
        _goalDebounce?.Cancel();
        _goalDebounce?.Dispose();
        _goalDebounce = null;

        foreach (Guid key in _actionDebounce.Keys.ToList())
        {
            _actionDebounce[key].Cancel();
            _actionDebounce[key].Dispose();
            _actionDebounce.Remove(key);
        }

        foreach (Guid key in _checkInDebounce.Keys.ToList())
        {
            _checkInDebounce[key].Cancel();
            _checkInDebounce[key].Dispose();
            _checkInDebounce.Remove(key);
        }
    }

    PersistIdentity CapturePersistIdentity(Guid? entityId = null) => new(
        _routeGeneration,
        Guid.TryParse(MemberId, out Guid memberId) ? memberId : Guid.Empty,
        Growth?.Id ?? Guid.Empty,
        Guid.TryParse(GoalId, out Guid goalId) ? goalId : Guid.Empty,
        entityId);

    bool IsActivePersistIdentity(PersistIdentity identity) =>
        !_disposed
        && !_lifetimeCts.IsCancellationRequested
        && identity.RouteGeneration == _routeGeneration
        && identity.MemberId != Guid.Empty
        && identity.GrowthId != Guid.Empty
        && identity.GoalId != Guid.Empty
        && Guid.TryParse(MemberId, out Guid memberId)
        && memberId == identity.MemberId
        && Guid.TryParse(GoalId, out Guid goalId)
        && goalId == identity.GoalId
        && Growth?.Id == identity.GrowthId;

    void SelectAction(Guid id)
    {
        _selectedActionId = id;
        _selectedCheckInId = null;
        _checkInValidationError = "";
        _actionValidationError = SelectedAction is null
            ? ""
            : GrowthUiHelpers.ValidateActionPersist(SelectedAction) ?? "";
    }

    void SelectCheckIn(Guid id)
    {
        _selectedCheckInId = id;
        _selectedActionId = null;
        _actionValidationError = "";
        UpdateCheckInValidation();
    }

    void UpdateCheckInValidation()
    {
        _checkInValidationError = SelectedCheckIn is null
            ? ""
            : GrowthUiHelpers.ValidateCheckInPersist(SelectedCheckIn) ?? "";
    }

    SemaphoreSlim GetActionPersistGate(Guid actionId)
    {
        lock (_persistGateLock)
        {
            if (!_actionPersistGates.TryGetValue(actionId, out SemaphoreSlim? gate))
            {
                gate = new SemaphoreSlim(1, 1);
                _actionPersistGates[actionId] = gate;
            }

            return gate;
        }
    }

    SemaphoreSlim GetCheckInPersistGate(Guid checkInId)
    {
        lock (_persistGateLock)
        {
            if (!_checkInPersistGates.TryGetValue(checkInId, out SemaphoreSlim? gate))
            {
                gate = new SemaphoreSlim(1, 1);
                _checkInPersistGates[checkInId] = gate;
            }

            return gate;
        }
    }

    GrowthGoal? GetGoalSnapshot(Guid goalId) =>
        Growth?.Goals.FirstOrDefault(g => g.Id == goalId);

    GrowthGoal? GetGoalSnapshot() =>
        Guid.TryParse(GoalId, out Guid gid) ? GetGoalSnapshot(gid) : null;

    GrowthGoalAction? GetActionSnapshot(Guid goalId, Guid actionId) =>
        GetGoalSnapshot(goalId)?.Actions.FirstOrDefault(a => a.Id == actionId);

    GrowthGoalCheckIn? GetCheckInSnapshot(Guid goalId, Guid checkInId) =>
        GetGoalSnapshot(goalId)?.CheckIns.FirstOrDefault(c => c.Id == checkInId);

    async Task RetryLoad()
    {
        if (!Guid.TryParse(MemberId, out Guid memberId))
            return;

        _retrying = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            await Cache.RetryGrowthLoadAsync(memberId);
        }
        finally
        {
            _retrying = false;
            _loadStatus = Cache.GetGrowthLoadStatus(memberId);
            _loadError = Cache.GetGrowthLoadError(memberId);
            if (!_disposed)
                await InvokeAsync(StateHasChanged);
        }
    }

    async Task ReloadGrowthAfterFailureAsync(PersistIdentity identity)
    {
        if (!IsActivePersistIdentity(identity))
            return;

        InvalidatePersistWork();

        if (Guid.TryParse(MemberId, out Guid memberId))
            await Cache.RetryGrowthLoadAsync(memberId);
    }

    void CommitGoal(Func<GrowthGoal, GrowthGoal> update, bool persistGoal = true, Guid? persistActionId = null, Guid? persistCheckInId = null)
    {
        if (!Guid.TryParse(MemberId, out Guid mid) || !Guid.TryParse(GoalId, out Guid gid) || Growth is null)
            return;

        GrowthGoal? currentGoal = Growth.Goals.FirstOrDefault(g => g.Id == gid);
        if (currentGoal is null)
            return;

        GrowthGoal baseGoal = CloneGoal(currentGoal);
        baseGoal.LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o");
        GrowthGoal nextGoal = update(baseGoal);

        if (persistActionId is { } aid)
        {
            GrowthGoalAction action = nextGoal.Actions.First(a => a.Id == aid);
            _actionValidationError = GrowthUiHelpers.ValidateActionPersist(action) ?? "";
            if (!string.IsNullOrEmpty(_actionValidationError))
                return;
            _goalValidationError = "";
            _checkInValidationError = "";
        }
        else if (persistCheckInId is { } cid)
        {
            GrowthGoalCheckIn checkIn = nextGoal.CheckIns.First(c => c.Id == cid);
            _checkInValidationError = GrowthUiHelpers.ValidateCheckInPersist(checkIn) ?? "";
            if (!string.IsNullOrEmpty(_checkInValidationError))
                return;
            _goalValidationError = "";
            _actionValidationError = "";
        }
        else if (persistGoal)
        {
            _goalValidationError = GrowthUiHelpers.ValidateGoalPersist(nextGoal) ?? "";
            if (!string.IsNullOrEmpty(_goalValidationError))
                return;
            _actionValidationError = "";
            _checkInValidationError = "";
        }

        Cache.UpdateGrowth(new Growth
        {
            Id = Growth.Id,
            MemberId = mid,
            SkillsInProgress = Growth.SkillsInProgress,
            FeedbackThemes = Growth.FeedbackThemes,
            FocusAreasMarkdown = Growth.FocusAreasMarkdown,
            Goals = Growth.Goals.Select(g => g.Id == gid ? nextGoal : g).ToList()
        });

        if (Growth.Id == Guid.Empty)
            return;

        if (persistActionId is { } actionId)
        {
            _actionPersistVersion[actionId] = _actionPersistVersion.GetValueOrDefault(actionId) + 1;
            long version = _actionPersistVersion[actionId];
            PersistIdentity identity = CapturePersistIdentity(actionId);
            DebounceAction(actionId, () => PersistActionAsync(identity, version));
            return;
        }

        if (persistCheckInId is { } checkInId)
        {
            _checkInPersistVersion[checkInId] = _checkInPersistVersion.GetValueOrDefault(checkInId) + 1;
            long version = _checkInPersistVersion[checkInId];
            PersistIdentity identity = CapturePersistIdentity(checkInId);
            DebounceCheckIn(checkInId, () => PersistCheckInAsync(identity, version));
            return;
        }

        if (persistGoal)
        {
            _goalPersistVersion++;
            long version = _goalPersistVersion;
            PersistIdentity identity = CapturePersistIdentity();
            DebounceGoal(() => PersistGoalAsync(identity, version));
        }
    }

    void OnStatusChange(ChangeEventArgs e) =>
        CommitGoal(g => { g.Status = Enum.Parse<GrowthGoalStatus>((string)e.Value!); return g; });

    void OnPriorityChange(ChangeEventArgs e)
    {
        CommitGoal(g =>
        {
            g.Priority = Enum.TryParse(e.Value?.ToString(), out Priority p) ? p : null;
            return g;
        });
    }

    void OnCategoryInput(ChangeEventArgs e) =>
        CommitGoal(g => { g.Category = string.IsNullOrWhiteSpace(e.Value?.ToString()) ? null : e.Value!.ToString(); return g; });

    void OnStartChange(ChangeEventArgs e) =>
        CommitGoal(g => { g.StartDateIso = string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString(); return g; });

    void OnTargetChange(ChangeEventArgs e) =>
        CommitGoal(g => { g.TargetDateIso = string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString(); return g; });

    void OnSummaryInput(ChangeEventArgs e) =>
        CommitGoal(g => { g.Summary = e.Value?.ToString(); return g; });

    void OnCriteriaInput(ChangeEventArgs e) =>
        CommitGoal(g =>
        {
            g.SuccessCriteria = (e.Value?.ToString() ?? "")
                .Split('\n')
                .Select(l => l.Trim())
                .Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"^-+\s*", ""))
                .Where(l => l.Length > 0)
                .ToList();
            return g;
        });

    void PatchAction(Guid actionId, Action<GrowthGoalAction> patch) =>
        CommitGoal(g =>
        {
            g.Actions = g.Actions.Select(a =>
            {
                if (a.Id != actionId) return a;
                GrowthGoalAction copy = CloneAction(a);
                patch(copy);
                return copy;
            }).ToList();
            return g;
        }, persistGoal: false, persistActionId: actionId);

    void OnActionTitleInput(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id) return;
        PatchAction(id, a => a.Title = e.Value?.ToString() ?? "");
    }

    void OnActionStateChange(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id) return;
        PatchAction(id, a => a.State = Enum.Parse<GrowthGoalActionState>((string)e.Value!));
    }

    void OnActionDueChange(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id) return;
        var v = e.Value?.ToString();
        PatchAction(id, a => a.DueDateIso = string.IsNullOrEmpty(v) ? null : v);
    }

    void OnActionPriorityChange(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id) return;
        PatchAction(id, a => a.Priority = Enum.TryParse(e.Value?.ToString(), out Priority p) ? p : Priority.Medium);
    }

    void OnActionNotesInput(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id) return;
        PatchAction(id, a => a.Notes = e.Value?.ToString());
    }

    void OnActionLinksInput(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id) return;
        List<string> links = (e.Value?.ToString() ?? "")
            .Split('\n')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        PatchAction(id, a => a.Links = links);
    }

    void OnActionDoneToggle(Guid actionId, ChangeEventArgs e)
    {
        var done = (bool)(e.Value ?? false);
        PatchAction(actionId, a => a.State = done ? GrowthGoalActionState.Complete : GrowthGoalActionState.Planned);
    }

    static string ActionStateLabel(GrowthGoalActionState state) => state switch
    {
        GrowthGoalActionState.InProgress => "In Progress",
        _ => state.ToString()
    };

    static string FormatOverviewDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return "—";
        string datePart = iso.Length >= 10 ? iso[..10] : iso;
        return DisplayLabels.FormatDateLabel(datePart);
    }

    static string GetNotesPreview(string? notes)
    {
        string[] lines = (notes ?? "").Trim().Split('\n');
        return lines.Length == 0 ? "" : lines[0];
    }

    static string GetCheckInPreview(string note) =>
        note.Length > 110 ? $"{note[..110]}…" : note;

    void OnCheckInDateChange(ChangeEventArgs e)
    {
        if (_selectedCheckInId is not { } id) return;
        PatchCheckIn(id, c => c.DateIso = e.Value?.ToString() ?? c.DateIso);
    }

    void OnCheckInSignalChange(ChangeEventArgs e)
    {
        if (_selectedCheckInId is not { } id) return;
        PatchCheckIn(id, c => c.Signal = Enum.Parse<GrowthGoalCheckInSignal>((string)e.Value!));
    }

    void OnCheckInNoteInput(ChangeEventArgs e)
    {
        if (_selectedCheckInId is not { } id) return;
        PatchCheckIn(id, c => c.Note = e.Value?.ToString() ?? "");
    }

    void PatchCheckIn(Guid checkInId, Action<GrowthGoalCheckIn> patch) =>
        CommitGoal(g =>
        {
            g.CheckIns = g.CheckIns.Select(c =>
            {
                if (c.Id != checkInId) return c;
                GrowthGoalCheckIn copy = CloneCheckIn(c);
                patch(copy);
                return copy;
            }).ToList();
            return g;
        }, persistGoal: false, persistCheckInId: checkInId);

    async Task AddAction()
    {
        if (Growth is null || !Guid.TryParse(GoalId, out Guid gid) || Growth.Id == Guid.Empty) return;
        try
        {
            AtlasApiDTOsGrowthGoalsActionsAddGrowthGoalActionResponse res = await GrowthService.AddActionAsync(Growth.Id, gid, new AtlasApiDTOsGrowthGoalsActionsAddGrowthGoalActionRequest
            {
                Title = "New action",
                State = AtlasDomainEnumsGrowthGoalActionState.Planned,
                Priority = Goal?.Priority is null ? AtlasDomainEnumsPriority.Medium : ApiMappers.ToApiPriority(Goal.Priority.Value)
            });
            if (!GrowthUiHelpers.IsValidCreatedId(res.Id))
            {
                await ReloadGrowthAfterFailureAsync(CapturePersistIdentity());
                await Dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("action"));
                return;
            }

            Guid id = res.Id!.Value;
            CommitGoal(g =>
            {
                List<GrowthGoalAction> actions = new()
                {
                    new GrowthGoalAction
                    {
                        Id = id,
                        Title = "New action",
                        State = GrowthGoalActionState.Planned,
                        Priority = g.Priority ?? Priority.Medium,
                        Notes = "",
                        Links = Array.Empty<string>()
                    }
                };
                actions.AddRange(g.Actions);
                g.Actions = actions;
                return g;
            }, persistGoal: false);
            _selectedActionId = id;
            _selectedCheckInId = null;
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add action.", ex));
        }
    }

    async Task AddCheckIn()
    {
        if (Growth is null || !Guid.TryParse(GoalId, out Guid gid) || Growth.Id == Guid.Empty) return;
        string today = DisplayLabels.TodayIsoDateLocal();
        try
        {
            AtlasApiDTOsGrowthGoalsCheckInsAddGrowthGoalCheckInResponse res = await GrowthService.AddCheckInAsync(Growth.Id, gid, new AtlasApiDTOsGrowthGoalsCheckInsAddGrowthGoalCheckInRequest
            {
                Date = DateTimeOffset.TryParse(today, out DateTimeOffset d) ? d : DateTimeOffset.Now,
                Signal = AtlasDomainEnumsGrowthGoalCheckInSignal.Mixed,
                Note = DefaultCheckInNote
            });
            if (!GrowthUiHelpers.IsValidCreatedId(res.Id))
            {
                await ReloadGrowthAfterFailureAsync(CapturePersistIdentity());
                await Dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("check-in"));
                return;
            }

            Guid id = res.Id!.Value;
            CommitGoal(g =>
            {
                List<GrowthGoalCheckIn> checkIns = new()
                {
                    new GrowthGoalCheckIn { Id = id, DateIso = today, Signal = GrowthGoalCheckInSignal.Mixed, Note = DefaultCheckInNote }
                };
                checkIns.AddRange(g.CheckIns);
                g.CheckIns = checkIns;
                return g;
            }, persistGoal: false);
            _selectedCheckInId = id;
            _selectedActionId = null;
            _checkInValidationError = "";
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add check-in.", ex));
        }
    }

    void DebounceGoal(Func<Task> action)
    {
        if (_disposed || _lifetimeCts.IsCancellationRequested)
            return;

        _goalDebounce?.Cancel();
        _goalDebounce?.Dispose();
        _goalDebounce = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
        CancellationToken token = _goalDebounce.Token;
        long routeGen = _routeGeneration;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, token);
                if (!token.IsCancellationRequested && !_disposed && routeGen == _routeGeneration)
                    await InvokeAsync(action);
            }
            catch (TaskCanceledException) { }
        });
    }

    void DebounceAction(Guid id, Func<Task> action)
    {
        if (_disposed || _lifetimeCts.IsCancellationRequested)
            return;

        if (_actionDebounce.TryGetValue(id, out CancellationTokenSource? existing))
        {
            existing.Cancel();
            existing.Dispose();
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
        _actionDebounce[id] = cts;
        long routeGen = _routeGeneration;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, cts.Token);
                if (!cts.Token.IsCancellationRequested && !_disposed && routeGen == _routeGeneration)
                    await InvokeAsync(action);
            }
            catch (TaskCanceledException) { }
        });
    }

    void DebounceCheckIn(Guid id, Func<Task> action)
    {
        if (_disposed || _lifetimeCts.IsCancellationRequested)
            return;

        if (_checkInDebounce.TryGetValue(id, out CancellationTokenSource? existing))
        {
            existing.Cancel();
            existing.Dispose();
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
        _checkInDebounce[id] = cts;
        long routeGen = _routeGeneration;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, cts.Token);
                if (!cts.Token.IsCancellationRequested && !_disposed && routeGen == _routeGeneration)
                    await InvokeAsync(action);
            }
            catch (TaskCanceledException) { }
        });
    }

    async Task PersistGoalAsync(PersistIdentity identity, long version)
    {
        if (_disposed || _lifetimeCts.IsCancellationRequested || !IsActivePersistIdentity(identity))
            return;

        try
        {
            await _goalPersistGate.WaitAsync(_lifetimeCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Exception? failure = null;
        var shouldRecover = false;

        try
        {
            while (!_disposed && !_lifetimeCts.IsCancellationRequested && IsActivePersistIdentity(identity))
            {
                if (version != _goalPersistVersion)
                    return;

                GrowthGoal? goal = GetGoalSnapshot(identity.GoalId);
                if (goal is null)
                    return;

                string? validation = GrowthUiHelpers.ValidateGoalPersist(goal);
                if (validation is not null)
                {
                    _goalValidationError = validation;
                    return;
                }

                _goalValidationError = "";

                await GrowthService.UpdateGoalAsync(identity.GrowthId, identity.GoalId, new AtlasApiDTOsGrowthGoalsUpdateGrowthGoalRequest
                {
                    Title = goal.Title,
                    Description = goal.Description,
                    Status = ApiMappers.ToApiGrowthGoalStatus(goal.Status),
                    StartDate = DateTimeOffset.TryParse(goal.StartDateIso, out DateTimeOffset sd) ? sd : null,
                    TargetDate = DateTimeOffset.TryParse(goal.TargetDateIso, out DateTimeOffset td) ? td : null,
                    Category = goal.Category,
                    Priority = goal.Priority is null ? null : ApiMappers.ToApiPriority(goal.Priority.Value),
                    ProgressPercent = goal.ProgressPercent,
                    Summary = goal.Summary,
                    SuccessCriteria = goal.SuccessCriteria.ToList()
                });

                if (!IsActivePersistIdentity(identity))
                    return;

                if (version == _goalPersistVersion)
                    return;

                version = _goalPersistVersion;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            if (version == _goalPersistVersion && !_disposed && IsActivePersistIdentity(identity))
            {
                failure = ex;
                shouldRecover = true;
            }
        }
        finally
        {
            _goalPersistGate.Release();
        }

        if (!shouldRecover || failure is null || _disposed || _lifetimeCts.IsCancellationRequested)
            return;

        await ReloadGrowthAfterFailureAsync(identity);
        await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to save goal changes.", failure));
    }

    async Task PersistActionAsync(PersistIdentity identity, long version)
    {
        if (_disposed || _lifetimeCts.IsCancellationRequested || !IsActivePersistIdentity(identity) || identity.EntityId is not { } actionId)
            return;

        SemaphoreSlim gate = GetActionPersistGate(actionId);
        try
        {
            await gate.WaitAsync(_lifetimeCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Exception? failure = null;
        var shouldRecover = false;

        try
        {
            while (!_disposed && !_lifetimeCts.IsCancellationRequested && IsActivePersistIdentity(identity))
            {
                if (!_actionPersistVersion.TryGetValue(actionId, out long current) || version != current)
                    return;

                GrowthGoalAction? action = GetActionSnapshot(identity.GoalId, actionId);
                if (action is null)
                    return;

                string? validation = GrowthUiHelpers.ValidateActionPersist(action);
                if (validation is not null)
                {
                    _actionValidationError = validation;
                    return;
                }

                _actionValidationError = "";

                await GrowthService.UpdateActionAsync(identity.GrowthId, identity.GoalId, action.Id, new AtlasApiDTOsGrowthGoalsActionsUpdateGrowthGoalActionRequest
                {
                    Title = action.Title,
                    State = ApiMappers.ToApiGrowthGoalActionState(action.State),
                    DueDate = DateTimeOffset.TryParse(action.DueDateIso, out DateTimeOffset d) ? d : null,
                    Priority = action.Priority is null ? null : ApiMappers.ToApiPriority(action.Priority.Value),
                    Notes = action.Notes,
                    Evidence = action.Links.Count > 0 ? string.Join('\n', action.Links) : null
                });

                if (!IsActivePersistIdentity(identity))
                    return;

                if (_actionPersistVersion.TryGetValue(actionId, out current) && current == version)
                    return;

                version = current;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            if (_actionPersistVersion.TryGetValue(actionId, out long current) && version == current && !_disposed && IsActivePersistIdentity(identity))
            {
                failure = ex;
                shouldRecover = true;
            }
        }
        finally
        {
            gate.Release();
        }

        if (!shouldRecover || failure is null || _disposed || _lifetimeCts.IsCancellationRequested)
            return;

        await ReloadGrowthAfterFailureAsync(identity);
        await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to save action changes.", failure));
    }

    async Task PersistCheckInAsync(PersistIdentity identity, long version)
    {
        if (_disposed || _lifetimeCts.IsCancellationRequested || !IsActivePersistIdentity(identity) || identity.EntityId is not { } checkInId)
            return;

        SemaphoreSlim gate = GetCheckInPersistGate(checkInId);
        try
        {
            await gate.WaitAsync(_lifetimeCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Exception? failure = null;
        var shouldRecover = false;

        try
        {
            while (!_disposed && !_lifetimeCts.IsCancellationRequested && IsActivePersistIdentity(identity))
            {
                if (!_checkInPersistVersion.TryGetValue(checkInId, out long current) || version != current)
                    return;

                GrowthGoalCheckIn? checkIn = GetCheckInSnapshot(identity.GoalId, checkInId);
                if (checkIn is null)
                    return;

                string? validation = GrowthUiHelpers.ValidateCheckInPersist(checkIn);
                if (validation is not null)
                {
                    _checkInValidationError = validation;
                    return;
                }

                _checkInValidationError = "";

                await GrowthService.UpdateCheckInAsync(identity.GrowthId, identity.GoalId, checkIn.Id, new AtlasApiDTOsGrowthGoalsCheckInsUpdateGrowthGoalCheckInRequest
                {
                    Date = DateTimeOffset.TryParse(checkIn.DateIso, out DateTimeOffset d) ? d : null,
                    Signal = ApiMappers.ToApiGrowthGoalCheckInSignal(checkIn.Signal),
                    Note = checkIn.Note.Trim()
                });

                if (!IsActivePersistIdentity(identity))
                    return;

                if (_checkInPersistVersion.TryGetValue(checkInId, out current) && current == version)
                    return;

                version = current;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            if (_checkInPersistVersion.TryGetValue(checkInId, out long current) && version == current && !_disposed && IsActivePersistIdentity(identity))
            {
                failure = ex;
                shouldRecover = true;
            }
        }
        finally
        {
            gate.Release();
        }

        if (!shouldRecover || failure is null || _disposed || _lifetimeCts.IsCancellationRequested)
            return;

        await ReloadGrowthAfterFailureAsync(identity);
        await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to save check-in changes.", failure));
    }

    static GrowthGoal CloneGoal(GrowthGoal g) => new()
    {
        Id = g.Id,
        Title = g.Title,
        Description = g.Description,
        Status = g.Status,
        Category = g.Category,
        Priority = g.Priority,
        StartDateIso = g.StartDateIso,
        TargetDateIso = g.TargetDateIso,
        LastUpdatedIso = g.LastUpdatedIso,
        ProgressPercent = g.ProgressPercent,
        Summary = g.Summary,
        SuccessCriteria = g.SuccessCriteria.ToList(),
        Actions = g.Actions.Select(CloneAction).ToList(),
        CheckIns = g.CheckIns.Select(CloneCheckIn).ToList()
    };

    static GrowthGoalAction CloneAction(GrowthGoalAction a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        DueDateIso = a.DueDateIso,
        State = a.State,
        Priority = a.Priority,
        Notes = a.Notes,
        Links = a.Links.ToList()
    };

    static GrowthGoalCheckIn CloneCheckIn(GrowthGoalCheckIn c) => new()
    {
        Id = c.Id,
        DateIso = c.DateIso,
        Signal = c.Signal,
        Note = c.Note
    };

    void OnChanged()
    {
        if (_disposed)
            return;

        if (Guid.TryParse(MemberId, out Guid id))
        {
            _loadStatus = Cache.GetGrowthLoadStatus(id);
            _loadError = Cache.GetGrowthLoadError(id);
            UpdateRouteInitializingFromLoadStatus();
        }

        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _disposed = true;
        _lifetimeCts.Cancel();
        Cache.Changed -= OnChanged;
        CancelAllDebounces();
        _lifetimeCts.Dispose();
    }
}
