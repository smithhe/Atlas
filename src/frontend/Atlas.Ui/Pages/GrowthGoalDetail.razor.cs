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
    private string _goalValidationError = "";
    private string _actionValidationError = "";
    private string _checkInValidationError = "";

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

    protected override async Task OnInitializedAsync()
    {
        Cache.Changed += OnChangedAsync;
        await Cache.EnsureHydratedAsync();
    }

    protected override async Task OnParametersSetAsync()
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

        if (Guid.TryParse(MemberId, out Guid memberId))
        {
            Selection.SelectTeamMember(memberId);
            _loadStatus = Cache.GetGrowthLoadStatus(memberId);
            _loadError = Cache.GetGrowthLoadError(memberId);

            if (memberId != _loadedGrowthMemberId)
            {
                _loadedGrowthMemberId = memberId;
                _routeInitializing = true;
                await Cache.EnsureGrowthLoadedAsync(memberId);
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

    private void InvalidateRouteState()
    {
        GrowthService.AbandonGoalPersists();
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
        if (!Guid.TryParse(MemberId, out Guid memberId))
        {
            return;
        }

        GrowthService.AbandonGoalPersists();
        await Cache.RetryGrowthLoadAsync(memberId);
    }

    private bool TryGetRouteIds(out Guid memberId, out Guid goalId)
    {
        memberId = Guid.Empty;
        goalId = Guid.Empty;
        return Guid.TryParse(MemberId, out memberId) && Guid.TryParse(GoalId, out goalId);
    }

    private void ApplyGoalUpdate(Func<GrowthGoal, GrowthGoal> update)
    {
        if (_disposed)
        {
            return;
        }

        if (!TryGetRouteIds(out Guid memberId, out Guid goalId))
        {
            return;
        }

        _goalValidationError = GrowthService.UpdateGoal(memberId, goalId, update) ?? "";
        if (string.IsNullOrEmpty(_goalValidationError))
        {
            _actionValidationError = "";
            _checkInValidationError = "";
        }
    }

    private void ApplyActionUpdate(Guid actionId, Action<GrowthGoalAction> patch)
    {
        if (_disposed)
        {
            return;
        }

        if (!TryGetRouteIds(out Guid memberId, out Guid goalId))
        {
            return;
        }

        _actionValidationError = GrowthService.UpdateAction(memberId, goalId, actionId, patch) ?? "";
        if (string.IsNullOrEmpty(_actionValidationError))
        {
            _goalValidationError = "";
            _checkInValidationError = "";
        }
    }

    private void ApplyCheckInUpdate(Guid checkInId, Action<GrowthGoalCheckIn> patch)
    {
        if (_disposed)
        {
            return;
        }

        if (!TryGetRouteIds(out Guid memberId, out Guid goalId))
        {
            return;
        }

        _checkInValidationError = GrowthService.UpdateCheckIn(memberId, goalId, checkInId, patch) ?? "";
        if (string.IsNullOrEmpty(_checkInValidationError))
        {
            _goalValidationError = "";
            _actionValidationError = "";
        }
    }

    private void OnStatusChange(ChangeEventArgs e) =>
        ApplyGoalUpdate(g => { g.Status = Enum.Parse<GrowthGoalStatus>((string)e.Value!); return g; });

    private void OnPriorityChange(ChangeEventArgs e) =>
        ApplyGoalUpdate(g =>
        {
            g.Priority = Enum.TryParse(e.Value?.ToString(), out Priority p) ? p : null;
            return g;
        });

    private void OnCategoryInput(ChangeEventArgs e) =>
        ApplyGoalUpdate(g =>
        {
            g.Category = string.IsNullOrWhiteSpace(e.Value?.ToString()) ? null : e.Value!.ToString();
            return g;
        });

    private void OnStartChange(ChangeEventArgs e) =>
        ApplyGoalUpdate(g =>
        {
            g.StartDateIso = string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString();
            return g;
        });

    private void OnTargetChange(ChangeEventArgs e) =>
        ApplyGoalUpdate(g =>
        {
            g.TargetDateIso = string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString();
            return g;
        });

    private void OnSummaryInput(ChangeEventArgs e) =>
        ApplyGoalUpdate(g => { g.Summary = e.Value?.ToString(); return g; });

    private void OnCriteriaInput(ChangeEventArgs e) =>
        ApplyGoalUpdate(g =>
        {
            g.SuccessCriteria = (e.Value?.ToString() ?? "")
                .Split('\n')
                .Select(l => l.Trim())
                .Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"^-+\s*", ""))
                .Where(l => l.Length > 0)
                .ToList();
            return g;
        });

    private void OnActionTitleInput(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        ApplyActionUpdate(id, a => a.Title = e.Value?.ToString() ?? "");
    }

    private void OnActionStateChange(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        ApplyActionUpdate(id, a => a.State = Enum.Parse<GrowthGoalActionState>((string)e.Value!));
    }

    private void OnActionDueChange(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        var v = e.Value?.ToString();
        ApplyActionUpdate(id, a => a.DueDateIso = string.IsNullOrEmpty(v) ? null : v);
    }

    private void OnActionPriorityChange(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        ApplyActionUpdate(id, a => a.Priority = Enum.TryParse(e.Value?.ToString(), out Priority p) ? p : Priority.Medium);
    }

    private void OnActionNotesInput(ChangeEventArgs e)
    {
        if (_selectedActionId is not { } id)
        {
            return;
        }

        ApplyActionUpdate(id, a => a.Notes = e.Value?.ToString());
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
        ApplyActionUpdate(id, a => a.Links = links);
    }

    private void OnActionDoneToggle(Guid actionId, ChangeEventArgs e)
    {
        var done = (bool)(e.Value ?? false);
        ApplyActionUpdate(actionId, a => a.State = done ? GrowthGoalActionState.Complete : GrowthGoalActionState.Planned);
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

        ApplyCheckInUpdate(id, c => c.DateIso = e.Value?.ToString() ?? c.DateIso);
    }

    private void OnCheckInSignalChange(ChangeEventArgs e)
    {
        if (_selectedCheckInId is not { } id)
        {
            return;
        }

        ApplyCheckInUpdate(id, c => c.Signal = Enum.Parse<GrowthGoalCheckInSignal>((string)e.Value!));
    }

    private void OnCheckInNoteInput(ChangeEventArgs e)
    {
        if (_selectedCheckInId is not { } id)
        {
            return;
        }

        ApplyCheckInUpdate(id, c => c.Note = e.Value?.ToString() ?? "");
    }

    private async Task AddAction()
    {
        if (_disposed)
        {
            return;
        }

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
            if (_disposed)
            {
                return;
            }

            if (!GrowthUiHelpers.IsValidCreatedId(created.Id))
            {
                await ReloadGrowthAfterFailureAsync();
                if (_disposed)
                {
                    return;
                }

                await Dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("action"));
                return;
            }

            _selectedActionId = created.Id;
            _selectedCheckInId = null;
        }
        catch (Exception ex)
        {
            if (_disposed)
            {
                return;
            }

            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add action.", ex));
        }
    }

    private async Task AddCheckIn()
    {
        if (_disposed)
        {
            return;
        }

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
            if (_disposed)
            {
                return;
            }

            if (!GrowthUiHelpers.IsValidCreatedId(created.Id))
            {
                await ReloadGrowthAfterFailureAsync();
                if (_disposed)
                {
                    return;
                }

                await Dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("check-in"));
                return;
            }

            _selectedCheckInId = created.Id;
            _selectedActionId = null;
            _checkInValidationError = "";
        }
        catch (Exception ex)
        {
            if (_disposed)
            {
                return;
            }

            await Dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add check-in.", ex));
        }
    }

    private async void OnChangedAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await InvokeAsync(() =>
            {
                if (Guid.TryParse(MemberId, out Guid id))
                {
                    _loadStatus = Cache.GetGrowthLoadStatus(id);
                    _loadError = Cache.GetGrowthLoadError(id);
                    UpdateRouteInitializingFromLoadStatus();
                }

                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    public void Dispose()
    {
        _disposed = true;
        GrowthService.AbandonGoalPersists();
        Cache.Changed -= OnChangedAsync;
    }
}
