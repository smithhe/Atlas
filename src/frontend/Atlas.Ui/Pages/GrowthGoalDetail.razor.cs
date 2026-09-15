using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class GrowthGoalDetail : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private SelectionState _selection { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IGrowthService _growthService { get; set; } = null!;

        private const string DefaultCheckInNote = "New check-in";

        [Parameter] public string? MemberId { get; set; }
        [Parameter] public string? GoalId { get; set; }

        private Guid? SelectedActionId { get; set; }
        private Guid? SelectedCheckInId { get; set; }
        private string? RouteMemberId { get; set; }
        private string? RouteGoalId { get; set; }
        private Guid LoadedGrowthMemberId { get; set; }
        private string ActionFilter { get; set; } = "All";
        private string ActionSort { get; set; } = "DueDate";
        private bool EditTimeframe { get; set; }
        private bool EditStatus { get; set; }
        private bool EditCategory { get; set; }
        private bool EditPriority { get; set; }
        private bool EditSummary { get; set; }
        private bool EditSuccessCriteria { get; set; }
        private GrowthLoadStatus LoadStatus { get; set; }
        private string? LoadError { get; set; }
        private bool Retrying { get; set; }
        private bool Disposed { get; set; }
        private bool RouteInitializing { get; set; }
        private string GoalValidationError { get; set; } = "";
        private string ActionValidationError { get; set; } = "";
        private string CheckInValidationError { get; set; } = "";

        private void BackToGrowth() => this._nav.NavigateTo($"/team/{MemberId}/growth");

        private TeamMember? Member =>
            Guid.TryParse(MemberId, out Guid id) ? this._cache.Team.FirstOrDefault(m => m.Id == id) : null;

        private Growth? Growth =>
            Guid.TryParse(MemberId, out Guid id) ? this._cache.GetGrowth(id) : null;

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
            this.SelectedActionId is { } id ? Goal?.Actions.FirstOrDefault(a => a.Id == id) : null;

        private GrowthGoalCheckIn? SelectedCheckIn =>
            this.SelectedCheckInId is { } id ? Goal?.CheckIns.FirstOrDefault(c => c.Id == id) : null;

        private int ProgressTotal => Goal?.Actions.Count ?? 0;
        private int ProgressDone => Goal?.Actions.Count(a => a.State == GrowthGoalActionState.Complete) ?? 0;
        private int ProgressPercent => ProgressTotal == 0 ? 0 : (int)Math.Round(100.0 * ProgressDone / ProgressTotal);

        private List<GrowthGoalAction> VisibleActions
        {
            get
            {
                IReadOnlyList<GrowthGoalAction> actions = Goal?.Actions ?? Array.Empty<GrowthGoalAction>();
                IEnumerable<GrowthGoalAction> filtered = this.ActionFilter == "All"
                    ? actions
                    : actions.Where(a => a.State.ToString() == this.ActionFilter);
                return filtered
                    .OrderBy(a =>
                    {
                        if (this.ActionSort == "State")
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
            this._cache.Changed += OnChangedAsync;
            await this._cache.EnsureHydratedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            var routeChanged = !string.Equals(this.RouteMemberId, MemberId, StringComparison.Ordinal)
                               || !string.Equals(this.RouteGoalId, GoalId, StringComparison.Ordinal);
            this.RouteMemberId = MemberId;
            this.RouteGoalId = GoalId;

            if (routeChanged)
            {
                InvalidateRouteState();
            }

            if (!string.IsNullOrEmpty(MemberId) && !Guid.TryParse(MemberId, out _))
            {
                this._nav.NavigateTo("/team", replace: true);
                return;
            }

            if (Guid.TryParse(MemberId, out Guid memberId))
            {
                this._selection.SelectTeamMember(memberId);
                this.LoadStatus = this._cache.GetGrowthLoadStatus(memberId);
                this.LoadError = this._cache.GetGrowthLoadError(memberId);

                if (memberId != this.LoadedGrowthMemberId)
                {
                    this.LoadedGrowthMemberId = memberId;
                    this.RouteInitializing = true;
                    await this._cache.EnsureGrowthLoadedAsync(memberId);
                }

                if (this._cache.TeamReady && Member is null)
                {
                    this._nav.NavigateTo("/team", replace: true);
                }

                UpdateRouteInitializingFromLoadStatus();
            }
        }

        private void UpdateRouteInitializingFromLoadStatus()
        {
            if (this.LoadStatus is GrowthLoadStatus.Succeeded or GrowthLoadStatus.Failed)
            {
                this.RouteInitializing = false;
            }
        }

        private void InvalidateRouteState()
        {
            this._growthService.AbandonGoalPersists();
            this.GoalValidationError = "";
            this.ActionValidationError = "";
            this.CheckInValidationError = "";
            this.Retrying = false;
            this.SelectedActionId = null;
            this.SelectedCheckInId = null;
            this.EditTimeframe = false;
            this.EditStatus = false;
            this.EditCategory = false;
            this.EditPriority = false;
            this.EditSummary = false;
            this.EditSuccessCriteria = false;
            this.RouteInitializing = true;
        }

        private void SelectAction(Guid id)
        {
            this.SelectedActionId = id;
            this.SelectedCheckInId = null;
            this.CheckInValidationError = "";
            this.ActionValidationError = SelectedAction is null
                ? ""
                : GrowthUiHelpers.ValidateActionPersist(SelectedAction) ?? "";
        }

        private void SelectCheckIn(Guid id)
        {
            this.SelectedCheckInId = id;
            this.SelectedActionId = null;
            this.ActionValidationError = "";
            UpdateCheckInValidation();
        }

        private void UpdateCheckInValidation()
        {
            this.CheckInValidationError = SelectedCheckIn is null
                ? ""
                : GrowthUiHelpers.ValidateCheckInPersist(SelectedCheckIn) ?? "";
        }

        private async Task RetryLoad()
        {
            if (!Guid.TryParse(MemberId, out Guid memberId))
            {
                return;
            }

            this.Retrying = true;
            await InvokeAsync(StateHasChanged);
            try
            {
                await this._cache.RetryGrowthLoadAsync(memberId);
            }
            finally
            {
                this.Retrying = false;
                this.LoadStatus = this._cache.GetGrowthLoadStatus(memberId);
                this.LoadError = this._cache.GetGrowthLoadError(memberId);
                if (!this.Disposed)
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

            this._growthService.AbandonGoalPersists();
            await this._cache.RetryGrowthLoadAsync(memberId);
        }

        private bool TryGetRouteIds(out Guid memberId, out Guid goalId)
        {
            memberId = Guid.Empty;
            goalId = Guid.Empty;
            return Guid.TryParse(MemberId, out memberId) && Guid.TryParse(GoalId, out goalId);
        }

        private void ApplyGoalUpdate(Func<GrowthGoal, GrowthGoal> update)
        {
            if (this.Disposed)
            {
                return;
            }

            if (!TryGetRouteIds(out Guid memberId, out Guid goalId))
            {
                return;
            }

            this.GoalValidationError = this._growthService.UpdateGoal(memberId, goalId, update) ?? "";
            if (string.IsNullOrEmpty(this.GoalValidationError))
            {
                this.ActionValidationError = "";
                this.CheckInValidationError = "";
            }
        }

        private void ApplyActionUpdate(Guid actionId, Action<GrowthGoalAction> patch)
        {
            if (this.Disposed)
            {
                return;
            }

            if (!TryGetRouteIds(out Guid memberId, out Guid goalId))
            {
                return;
            }

            this.ActionValidationError = this._growthService.UpdateAction(memberId, goalId, actionId, patch) ?? "";
            if (string.IsNullOrEmpty(this.ActionValidationError))
            {
                this.GoalValidationError = "";
                this.CheckInValidationError = "";
            }
        }

        private void ApplyCheckInUpdate(Guid checkInId, Action<GrowthGoalCheckIn> patch)
        {
            if (this.Disposed)
            {
                return;
            }

            if (!TryGetRouteIds(out Guid memberId, out Guid goalId))
            {
                return;
            }

            this.CheckInValidationError = this._growthService.UpdateCheckIn(memberId, goalId, checkInId, patch) ?? "";
            if (string.IsNullOrEmpty(this.CheckInValidationError))
            {
                this.GoalValidationError = "";
                this.ActionValidationError = "";
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
            if (this.SelectedActionId is not { } id)
            {
                return;
            }

            ApplyActionUpdate(id, a => a.Title = e.Value?.ToString() ?? "");
        }

        private void OnActionStateChange(ChangeEventArgs e)
        {
            if (this.SelectedActionId is not { } id)
            {
                return;
            }

            ApplyActionUpdate(id, a => a.State = Enum.Parse<GrowthGoalActionState>((string)e.Value!));
        }

        private void OnActionDueChange(ChangeEventArgs e)
        {
            if (this.SelectedActionId is not { } id)
            {
                return;
            }

            var v = e.Value?.ToString();
            ApplyActionUpdate(id, a => a.DueDateIso = string.IsNullOrEmpty(v) ? null : v);
        }

        private void OnActionPriorityChange(ChangeEventArgs e)
        {
            if (this.SelectedActionId is not { } id)
            {
                return;
            }

            ApplyActionUpdate(id, a => a.Priority = Enum.TryParse(e.Value?.ToString(), out Priority p) ? p : Priority.Medium);
        }

        private void OnActionNotesInput(ChangeEventArgs e)
        {
            if (this.SelectedActionId is not { } id)
            {
                return;
            }

            ApplyActionUpdate(id, a => a.Notes = e.Value?.ToString());
        }

        private void OnActionLinksInput(ChangeEventArgs e)
        {
            if (this.SelectedActionId is not { } id)
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
            if (this.SelectedCheckInId is not { } id)
            {
                return;
            }

            ApplyCheckInUpdate(id, c => c.DateIso = e.Value?.ToString() ?? c.DateIso);
        }

        private void OnCheckInSignalChange(ChangeEventArgs e)
        {
            if (this.SelectedCheckInId is not { } id)
            {
                return;
            }

            ApplyCheckInUpdate(id, c => c.Signal = Enum.Parse<GrowthGoalCheckInSignal>((string)e.Value!));
        }

        private void OnCheckInNoteInput(ChangeEventArgs e)
        {
            if (this.SelectedCheckInId is not { } id)
            {
                return;
            }

            ApplyCheckInUpdate(id, c => c.Note = e.Value?.ToString() ?? "");
        }

        private async Task AddAction()
        {
            if (this.Disposed)
            {
                return;
            }

            if (Growth is null || !Guid.TryParse(MemberId, out Guid memberId) || !Guid.TryParse(GoalId, out Guid gid) || Growth.Id == Guid.Empty)
            {
                return;
            }

            try
            {
                GrowthGoalAction created = await this._growthService.AddActionAsync(memberId, Growth.Id, gid, new GrowthGoalAction
                {
                    Title = "New action",
                    State = GrowthGoalActionState.Planned,
                    Priority = Goal?.Priority ?? Priority.Medium,
                    Notes = "",
                    Links = Array.Empty<string>()
                });
                if (this.Disposed)
                {
                    return;
                }

                if (!GrowthUiHelpers.IsValidCreatedId(created.Id))
                {
                    await ReloadGrowthAfterFailureAsync();
                    if (this.Disposed)
                    {
                        return;
                    }

                    await this._dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("action"));
                    return;
                }

                this.SelectedActionId = created.Id;
                this.SelectedCheckInId = null;
            }
            catch (Exception ex)
            {
                if (this.Disposed)
                {
                    return;
                }

                await this._dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add action.", ex));
            }
        }

        private async Task AddCheckIn()
        {
            if (this.Disposed)
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
                GrowthGoalCheckIn created = await this._growthService.AddCheckInAsync(memberId, Growth.Id, gid, new GrowthGoalCheckIn
                {
                    DateIso = today,
                    Signal = GrowthGoalCheckInSignal.Mixed,
                    Note = DefaultCheckInNote
                });
                if (this.Disposed)
                {
                    return;
                }

                if (!GrowthUiHelpers.IsValidCreatedId(created.Id))
                {
                    await ReloadGrowthAfterFailureAsync();
                    if (this.Disposed)
                    {
                        return;
                    }

                    await this._dialogs.AlertAsync(GrowthUiHelpers.MissingCreatedIdMessage("check-in"));
                    return;
                }

                this.SelectedCheckInId = created.Id;
                this.SelectedActionId = null;
                this.CheckInValidationError = "";
            }
            catch (Exception ex)
            {
                if (this.Disposed)
                {
                    return;
                }

                await this._dialogs.AlertAsync(GrowthUiHelpers.FormatUserError("Unable to add check-in.", ex));
            }
        }

        private async void OnChangedAsync()
        {
            if (this.Disposed)
            {
                return;
            }

            try
            {
                await InvokeAsync(() =>
                {
                    if (Guid.TryParse(MemberId, out Guid id))
                    {
                        this.LoadStatus = this._cache.GetGrowthLoadStatus(id);
                        this.LoadError = this._cache.GetGrowthLoadError(id);
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
            this.Disposed = true;
            this._growthService.AbandonGoalPersists();
            this._cache.Changed -= OnChangedAsync;
        }
    }
}
