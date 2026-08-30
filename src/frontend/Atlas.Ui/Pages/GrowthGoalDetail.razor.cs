using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class GrowthGoalDetail : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private SelectionState Selection { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private GrowthService GrowthService { get; set; } = null!;

    private const string DefaultCheckInNote = "New check-in";

    [Parameter] public string? MemberId { get; set; }
    [Parameter] public string? GoalId { get; set; }

    private Guid? _selectedActionId;
    private Guid? _selectedCheckInId;
    private string? _routeMemberId;
    private string? _routeGoalId;
    private Guid _loadedGrowthMemberId;
    private string _actionFilter = "All";
    private string _actionSort = "DueDate";
    private bool _editTimeframe;
    private bool _editStatus;
    private bool _editCategory;
    private bool _editPriority;
    private bool _editSummary;
    private bool _editSuccessCriteria;
    private GrowthLoadStatus _loadStatus;
    private string? _loadError;
    private bool _retrying;
    private bool _disposed;
    private bool _routeInitializing;
    private long _routeGeneration;
    private string _goalValidationError = "";
    private string _actionValidationError = "";
    private string _checkInValidationError = "";
    private readonly CancellationTokenSource _lifetimeCts = new();
    private KeyedDebounceGate? _debounceGate;

    private void BackToGrowth() => Nav.NavigateTo($"/team/{MemberId}/growth");

    private TeamMember? Member =>
        Guid.TryParse(MemberId, out Guid id) ? Cache.Team.FirstOrDefault(m => m.Id == id) : null;

    private Growth? Growth =>
        Guid.TryParse(MemberId, out Guid id) ? Cache.GetGrowth(id) : null;

    private GrowthGoal? Goal
    {
        get
        {
            if (Growth is null || !Guid.TryParse(GoalId, out Guid gid))
            {
                return null;
            }

            return Growth.Goals.FirstOrDefault(g => g.Id == gid);
        }
    }

    private GrowthGoalAction? SelectedAction =>
        _selectedActionId is { } id ? Goal?.Actions.FirstOrDefault(a => a.Id == id) : null;

    private GrowthGoalCheckIn? SelectedCheckIn =>
        _selectedCheckInId is { } id ? Goal?.CheckIns.FirstOrDefault(c => c.Id == id) : null;

    private int ProgressTotal => Goal?.Actions.Count ?? 0;
    private int ProgressDone => Goal?.Actions.Count(a => a.State == GrowthGoalActionState.Complete) ?? 0;
    private int ProgressPercent => ProgressTotal == 0 ? 0 : (int)Math.Round(100.0 * ProgressDone / ProgressTotal);

    private List<GrowthGoalAction> VisibleActions
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
                    {
                        return a.State switch
                        {
                            GrowthGoalActionState.InProgress => 0,
                            GrowthGoalActionState.Planned => 1,
                            _ => 2
                        };
                    }

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
        _debounceGate = new KeyedDebounceGate(_lifetimeCts.Token);
        _ = Cache.EnsureHydratedAsync();
    }

    protected override void OnParametersSet()
    {
        var routeChanged = !string.Equals(_routeMemberId, MemberId, StringComparison.Ordinal)
                           || !string.Equals(_routeGoalId, GoalId, StringComparison.Ordinal);
        _routeMemberId = MemberId;
        _routeGoalId = GoalId;

        if (routeChanged)
        {
            InvalidateRouteState();
        }

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
            {
                Nav.NavigateTo("/team", replace: true);
            }

            UpdateRouteInitializingFromLoadStatus();
        }
    }

    private void UpdateRouteInitializingFromLoadStatus()
    {
        if (_loadStatus is GrowthLoadStatus.Succeeded or GrowthLoadStatus.Failed)
        {
            _routeInitializing = false;
        }
    }

    private void InvalidatePersistWork() => _debounceGate?.InvalidateAll();

    private void InvalidateRouteState()
    {
        InvalidatePersistWork();
        _routeGeneration++;
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

    private bool IsActiveForPersist() =>
        !_disposed
        && !_lifetimeCts.IsCancellationRequested
        && Guid.TryParse(MemberId, out _)
        && Guid.TryParse(GoalId, out _)
        && Growth?.Id != Guid.Empty;

    private void SchedulePersist(KeyedDebounceGate.Key key, Func<Task> persist, string failureMessage)
    {
        if (_debounceGate is null)
        {
            return;
        }

        long version = _debounceGate.BumpVersion(key);
        var routeGen = _routeGeneration;
        _debounceGate.DebounceKeyed(
            key,
            version,
            routeGen,
            () => _routeGeneration,
            IsActiveForPersist,
            async work => await InvokeAsync(work),
            async () =>
            {
                try
                {
                    await persist();
                }
                catch (Exception ex)
                {
                    if (_disposed || _lifetimeCts.IsCancellationRequested || !IsActiveForPersist())
                    {
                        return;
                    }

                    await ReloadGrowthAfterFailureAsync();
                    await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError(failureMessage, ex));
                }
            });
    }

    private void SelectAction(Guid id)
    {
        _selectedActionId = id;
        _selectedCheckInId = null;
        _checkInValidationError = "";
        _actionValidationError = SelectedAction is null
            ? ""
            : GrowthUiHelpers.ValidateActionPersist(SelectedAction) ?? "";
    }

    private void SelectCheckIn(Guid id)
    {
        _selectedCheckInId = id;
        _selectedActionId = null;
        _actionValidationError = "";
        UpdateCheckInValidation();
    }

    private void UpdateCheckInValidation()
    {
        _checkInValidationError = SelectedCheckIn is null
            ? ""
            : GrowthUiHelpers.ValidateCheckInPersist(SelectedCheckIn) ?? "";
    }

    private GrowthGoal? GetGoalSnapshot(Guid goalId) =>
        Growth?.Goals.FirstOrDefault(g => g.Id == goalId);

    private GrowthGoal? GetGoalSnapshot() =>
        Guid.TryParse(GoalId, out Guid gid) ? GetGoalSnapshot(gid) : null;

    private GrowthGoalAction? GetActionSnapshot(Guid goalId, Guid actionId) =>
        GetGoalSnapshot(goalId)?.Actions.FirstOrDefault(a => a.Id == actionId);

    private GrowthGoalCheckIn? GetCheckInSnapshot(Guid goalId, Guid checkInId) =>
        GetGoalSnapshot(goalId)?.CheckIns.FirstOrDefault(c => c.Id == checkInId);

    private async Task RetryLoad()
    {
        if (!Guid.TryParse(MemberId, out Guid memberId))
        {
            return;
        }

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
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task ReloadGrowthAfterFailureAsync()
    {
        if (!IsActiveForPersist())
        {
            return;
        }

        InvalidatePersistWork();

        if (Guid.TryParse(MemberId, out Guid memberId))
        {
            await Cache.RetryGrowthLoadAsync(memberId);
        }
    }

    private void CommitGoal(Func<GrowthGoal, GrowthGoal> update, bool persistGoal = true, Guid? persistActionId = null, Guid? persistCheckInId = null)
    {
        if (!Guid.TryParse(MemberId, out Guid mid) || !Guid.TryParse(GoalId, out Guid gid) || Growth is null)
        {
            return;
        }

        GrowthGoal? currentGoal = Growth.Goals.FirstOrDefault(g => g.Id == gid);
        if (currentGoal is null)
        {
            return;
        }

        GrowthGoal baseGoal = CloneGoal(currentGoal);
        baseGoal.LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o");
        GrowthGoal nextGoal = update(baseGoal);

        if (persistActionId is { } aid)
        {
            GrowthGoalAction action = nextGoal.Actions.First(a => a.Id == aid);
            _actionValidationError = GrowthUiHelpers.ValidateActionPersist(action) ?? "";
            if (!string.IsNullOrEmpty(_actionValidationError))
            {
                return;
            }

            _goalValidationError = "";
            _checkInValidationError = "";
        }
        else if (persistCheckInId is { } cid)
        {
            GrowthGoalCheckIn checkIn = nextGoal.CheckIns.First(c => c.Id == cid);
            _checkInValidationError = GrowthUiHelpers.ValidateCheckInPersist(checkIn) ?? "";
            if (!string.IsNullOrEmpty(_checkInValidationError))
            {
                return;
            }

            _goalValidationError = "";
            _actionValidationError = "";
        }
        else if (persistGoal)
        {
            _goalValidationError = GrowthUiHelpers.ValidateGoalPersist(nextGoal) ?? "";
            if (!string.IsNullOrEmpty(_goalValidationError))
            {
                return;
            }

            _actionValidationError = "";
            _checkInValidationError = "";
        }

        Cache.UpdateGrowth(EntityClone.ReplaceGoal(Growth, gid, _ => nextGoal));

        if (Growth.Id == Guid.Empty)
        {
            return;
        }

        if (persistActionId is { } actionId)
        {
            SchedulePersist(
                new KeyedDebounceGate.Key("action", actionId),
                () =>
                {
                    GrowthGoalAction? action = GetActionSnapshot(gid, actionId);
                    return action is null
                        ? Task.CompletedTask
                        : GrowthService.PersistActionHttpAsync(Growth.Id, gid, action, _lifetimeCts.Token);
                },
                "Unable to save action changes.");
            return;
        }

        if (persistCheckInId is { } checkInId)
        {
            SchedulePersist(
                new KeyedDebounceGate.Key("checkin", checkInId),
                () =>
                {
                    GrowthGoalCheckIn? checkIn = GetCheckInSnapshot(gid, checkInId);
                    return checkIn is null
                        ? Task.CompletedTask
                        : GrowthService.PersistCheckInHttpAsync(Growth.Id, gid, checkIn, _lifetimeCts.Token);
                },
                "Unable to save check-in changes.");
            return;
        }

        if (persistGoal)
        {
            SchedulePersist(
                new KeyedDebounceGate.Key("goal", gid),
                () =>
                {
                    GrowthGoal? goal = GetGoalSnapshot(gid);
                    return goal is null
                        ? Task.CompletedTask
                        : GrowthService.PersistGoalHttpAsync(Growth.Id, goal, _lifetimeCts.Token);
                },
                "Unable to save goal changes.");
        }
    }

    private void OnStatusChange(ChangeEventArgs e) =>
        CommitGoal(g => { g.Status = Enum.Parse<GrowthGoalStatus>((string)e.Value!); return g; });

    private void OnPriorityChange(ChangeEventArgs e)
    {
        CommitGoal(g =>
        {
            g.Priority = Enum.TryParse(e.Value?.ToString(), out Priority p) ? p : null;
            return g;
        });
    }

    private void OnCategoryInput(ChangeEventArgs e) =>
        CommitGoal(g => { g.Category = string.IsNullOrWhiteSpace(e.Value?.ToString()) ? null : e.Value!.ToString(); return g; });

    private void OnStartChange(ChangeEventArgs e) =>
        CommitGoal(g => { g.StartDateIso = string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString(); return g; });

    private void OnTargetChange(ChangeEventArgs e) =>
        CommitGoal(g => { g.TargetDateIso = string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString(); return g; });

    private void OnSummaryInput(ChangeEventArgs e) =>
        CommitGoal(g => { g.Summary = e.Value?.ToString(); return g; });

    private void OnCriteriaInput(ChangeEventArgs e) =>
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

    private void PatchAction(Guid actionId, Action<GrowthGoalAction> patch) =>
        CommitGoal(g =>
        {
            g.Actions = g.Actions.Select(a =>
            {
                if (a.Id != actionId)
                {
                    return a;
                }

                GrowthGoalAction copy = CloneAction(a);
                patch(copy);
                return copy;
            }).ToList();
            return g;
        }, persistGoal: false, persistActionId: actionId);

    private void OnActionTitleInput(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        PatchAction(id, a => a.Title = e.Value?.ToString() ?? "");
    }

    private void OnActionStateChange(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        PatchAction(id, a => a.State = Enum.Parse<GrowthGoalActionState>((string)e.Value!));
    }

    private void OnActionDueChange(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        var v = e.Value?.ToString();
        PatchAction(id, a => a.DueDateIso = string.IsNullOrEmpty(v) ? null : v);
    }

    private void OnActionPriorityChange(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        PatchAction(id, a => a.Priority = Enum.TryParse(e.Value?.ToString(), out Priority p) ? p : Priority.Medium);
    }

    private void OnActionNotesInput(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        PatchAction(id, a => a.Notes = e.Value?.ToString());
    }

    private void OnActionLinksInput(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        var links = (e.Value?.ToString() ?? "")
            .Split('\n')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        PatchAction(id, a => a.Links = links);
    }

    private void OnActionDoneToggle(Guid actionId, ChangeEventArgs e)
    {
        var done = (bool)(e.Value ?? false);
        PatchAction(actionId, a => a.State = done ? GrowthGoalActionState.Complete : GrowthGoalActionState.Planned);
    }

    private static string ActionStateLabel(GrowthGoalActionState state) => state switch
    {
        GrowthGoalActionState.InProgress => "In Progress",
        _ => state.ToString()
    };

    private static string FormatOverviewDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
        {
            return "—";
        }

        var datePart = iso.Length >= 10 ? iso[..10] : iso;
        return DisplayLabels.FormatDateLabel(datePart);
    }

    private static string GetNotesPreview(string? notes)
    {
        var lines = (notes ?? "").Trim().Split('\n');
        return lines.Length == 0 ? "" : lines[0];
    }

    private static string GetCheckInPreview(string note) =>
        note.Length > 110 ? $"{note[..110]}…" : note;

    private void OnCheckInDateChange(ChangeEventArgs e)
    {
        if (_selectedCheckInId is not { } id)
        {
            return;
        }

        PatchCheckIn(id, c => c.DateIso = e.Value?.ToString() ?? c.DateIso);
    }

    private void OnCheckInSignalChange(ChangeEventArgs e)
    {
        if (_selectedCheckInId is not { } id)
        {
            return;
        }

        PatchCheckIn(id, c => c.Signal = Enum.Parse<GrowthGoalCheckInSignal>((string)e.Value!));
    }

    private void OnCheckInNoteInput(ChangeEventArgs e)
    {
        if (_selectedCheckInId is not { } id)
        {
            return;
        }

        PatchCheckIn(id, c => c.Note = e.Value?.ToString() ?? "");
    }

    private void PatchCheckIn(Guid checkInId, Action<GrowthGoalCheckIn> patch) =>
        CommitGoal(g =>
        {
            g.CheckIns = g.CheckIns.Select(c =>
            {
                if (c.Id != checkInId)
                {
                    return c;
                }

                GrowthGoalCheckIn copy = CloneCheckIn(c);
                patch(copy);
                return copy;
            }).ToList();
            return g;
        }, persistGoal: false, persistCheckInId: checkInId);

    private async Task AddAction()
    {
        if (Growth is null || !Guid.TryParse(MemberId, out Guid memberId) || !Guid.TryParse(GoalId, out Guid gid) || Growth.Id == Guid.Empty)
        {
            return;
        }

        try
        {
            GrowthGoalAction created = await GrowthService.AddActionAsync(memberId, Growth.Id, gid, new GrowthGoalAction
            {
                Title = "New action",
                State = GrowthGoalActionState.Planned,
                Priority = Goal?.Priority ?? Priority.Medium,
                Notes = "",
                Links = Array.Empty<string>()
            });
            if (!GrowthUiHelpers.IsValidCreatedId(created.Id))
            {
                await ReloadGrowthAfterFailureAsync();
                await Dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("action"));
                return;
            }

            _selectedActionId = created.Id;
            _selectedCheckInId = null;
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add action.", ex));
        }
    }

    private async Task AddCheckIn()
    {
        if (Growth is null || !Guid.TryParse(MemberId, out Guid memberId) || !Guid.TryParse(GoalId, out Guid gid) || Growth.Id == Guid.Empty)
        {
            return;
        }

        var today = DisplayLabels.TodayIsoDateLocal();
        try
        {
            GrowthGoalCheckIn created = await GrowthService.AddCheckInAsync(memberId, Growth.Id, gid, new GrowthGoalCheckIn
            {
                DateIso = today,
                Signal = GrowthGoalCheckInSignal.Mixed,
                Note = DefaultCheckInNote
            });
            if (!GrowthUiHelpers.IsValidCreatedId(created.Id))
            {
                await ReloadGrowthAfterFailureAsync();
                await Dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("check-in"));
                return;
            }

            _selectedCheckInId = created.Id;
            _selectedActionId = null;
            _checkInValidationError = "";
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add check-in.", ex));
        }
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

    private void OnChanged()
    {
        if (_disposed)
        {
            return;
        }

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
        _debounceGate?.Dispose();
        _lifetimeCts.Dispose();
    }
}
