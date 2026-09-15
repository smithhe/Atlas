using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Pages
{
    public partial class Tasks : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private SelectionState _selection { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IAiStateService _ai { get; set; } = null!;
        [Inject] private ITaskService _taskService { get; set; } = null!;

        [Parameter] public string? TaskId { get; set; }

        private bool Creating { get; set; }
        private Guid? AutoEditId { get; set; }

        private string ProjectFilter { get; set; } = "All";
        private string RiskFilter { get; set; } = "All";
        private string StatusFilter { get; set; } = "All";
        private string PriorityFilter { get; set; } = "All";
        private string StalenessFilter { get; set; } = "All";
        private string DurationFilter { get; set; } = "All";
        private string SortBy { get; set; } = "Priority";
        private string SortDir { get; set; } = "Desc";

        private bool IsFocusMode => !string.IsNullOrEmpty(TaskId);
        private bool ShowDetail => IsFocusMode || this._selection.SelectedTaskId is not null;
        private int StaleDays => this._cache.Settings?.StaleDays ?? 10;
        private int WarnStart => Math.Max(1, StaleDays - 2);

        private IReadOnlyList<Project> ProjectOptions =>
            this._cache.Projects.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();

        private IReadOnlyList<Risk> RiskOptions =>
            this._cache.Risks.OrderBy(r => r.Title, StringComparer.Ordinal).ToList();

        private Dictionary<Guid, TeamMember> MemberById =>
            this._cache.Team.ToDictionary(m => m.Id);

        private Dictionary<Guid, AtlasTask> TaskById =>
            this._cache.Tasks.ToDictionary(t => t.Id);

        private AtlasTask? Selected
        {
            get
            {
                Guid? id = IsFocusMode && Guid.TryParse(TaskId, out Guid focusId)
                    ? focusId
                    : this._selection.SelectedTaskId;
                if (id is null)
                {
                    return null;
                }

                return this._cache.Tasks.FirstOrDefault(t => t.Id == id);
            }
        }

        private IReadOnlyList<AtlasTask> FilteredSorted => BuildFilteredSorted();

        protected override async Task OnInitializedAsync()
        {
            this._cache.Changed += OnChangedAsync;
            this._selection.Changed += OnChangedAsync;
            this._ai.SetContext("Context: Tasks",
            [
                new AiAction("suggest-next-task", "Suggest Next Task"),
                new AiAction("summarize-week", "Summarize Incomplete Work (week)"),
                new AiAction("reprioritize", "Reprioritize suggestions"),
            ]);
            await this._cache.EnsureHydratedAsync();
        }

        protected override void OnParametersSet()
        {
            if (IsFocusMode && Guid.TryParse(TaskId, out Guid id))
            {
                this._selection.SelectTask(id);
                if (this._cache.TasksReady && this._cache.Tasks.All(t => t.Id != id))
                {
                    this._nav.NavigateTo("/tasks", replace: true);
                }
            }
        }

        private async void OnChangedAsync()
        {
            try
            {
                await InvokeAsync(() =>
                {
                    if (IsFocusMode && Guid.TryParse(TaskId, out Guid id) && this._cache.TasksReady && this._cache.Tasks.All(t => t.Id != id))
                    {
                        this._nav.NavigateTo("/tasks", replace: true);
                        return;
                    }

                    StateHasChanged();
                });
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        }

        private void SelectFromList(Guid id)
        {
            this._selection.SelectTask(id);
        }

        private void CloseDetail() => this._selection.SelectTask(null);

        private void ClearAutoEdit() => this.AutoEditId = null;

        private async Task OnTaskDeleted()
        {
            if (IsFocusMode)
            {
                this._nav.NavigateTo("/tasks", replace: true);
            }

            if (Selected is not null && this.AutoEditId == Selected.Id)
            {
                this.AutoEditId = null;
            }
        }

        private void GoFocus(Guid id) => this._nav.NavigateTo($"/tasks/{id}");

        private void EnterFocus()
        {
            if (Selected is null)
            {
                return;
            }

            this._nav.NavigateTo($"/tasks/{Selected.Id}");
        }

        private void ExitFocus() => this._nav.NavigateTo("/tasks");

        private async Task HandleAddTask()
        {
            if (this.Creating)
            {
                return;
            }

            this.Creating = true;
            try
            {
                var draft = new AtlasTask
                {
                    Title = "New task",
                    Priority = Priority.Medium,
                    Status = Models.TaskStatus.NotStarted,
                    EstimatedDurationText = "1h",
                    EstimateConfidence = Confidence.Medium,
                    Notes = "",
                    DependencyTaskIds = Array.Empty<Guid>(),
                    LastTouchedIso = DateTimeOffset.UtcNow.ToString("o")
                };
                AtlasTask created = await this._taskService.CreateAsync(draft);
                Guid id = created.Id;
                this.AutoEditId = id;
                this._selection.SelectTask(id);
            }
            catch (Exception)
            {
                await this._dialogs.AlertAsync("Unable to create task right now. Please try again.");
            }
            finally
            {
                this.Creating = false;
            }
        }

        private void OnProjectFilterChange(ChangeEventArgs e) => this.ProjectFilter = e.Value?.ToString() ?? "All";
        private void OnRiskFilterChange(ChangeEventArgs e) => this.RiskFilter = e.Value?.ToString() ?? "All";
        private void OnStatusFilterChange(ChangeEventArgs e) => this.StatusFilter = e.Value?.ToString() ?? "All";
        private void OnPriorityFilterChange(ChangeEventArgs e) => this.PriorityFilter = e.Value?.ToString() ?? "All";
        private void OnStalenessFilterChange(ChangeEventArgs e) => this.StalenessFilter = e.Value?.ToString() ?? "All";
        private void OnDurationFilterChange(ChangeEventArgs e) => this.DurationFilter = e.Value?.ToString() ?? "All";

        private string SortButtonGlyph(string category) =>
            this.SortBy != category ? "↕" : this.SortDir == "Asc" ? "▲" : "▼";

        private void ToggleSort(string category)
        {
            if (this.SortBy == category)
            {
                this.SortDir = this.SortDir == "Asc" ? "Desc" : "Asc";
            }
            else
            {
                this.SortBy = category;
                this.SortDir = category is "Project" or "Risk" or "Title" ? "Asc" : "Desc";
            }
        }

        private string ActivityColor(int days) =>
            days >= StaleDays ? "red" : days >= WarnStart ? "yellow" : "green";

        private int BlockedByCount(AtlasTask task) =>
            task.DependencyTaskIds.Count(id =>
                TaskById.TryGetValue(id, out AtlasTask? dep) && dep.Status != Models.TaskStatus.Done);

        private static string ListDurationPill(string text)
        {
            Duration.ParsedDuration? parsed = Duration.ParseDurationText(text);
            return Duration.FormatDurationFromMinutes(parsed?.TotalMinutes ?? 0);
        }

        private IReadOnlyList<AtlasTask> BuildFilteredSorted()
        {
            var indexed = this._cache.Tasks.Where(PassesFilters).Select((t, i) => (t, i)).ToList();
            var dir = this.SortDir == "Asc" ? 1 : -1;
            indexed.Sort((a, b) =>
            {
                var c = CompareTasks(a.t, b.t, dir);
                return c != 0 ? c : a.i.CompareTo(b.i);
            });
            return indexed.Select(x => x.t).ToList();
        }

        private bool PassesFilters(AtlasTask t)
        {
            if (this.StatusFilter != "All" && DisplayLabels.FormatTaskStatus(t.Status) != this.StatusFilter)
            {
                return false;
            }

            if (this.PriorityFilter != "All" && t.Priority.ToString() != this.PriorityFilter)
            {
                return false;
            }

            if (!EntityIdMatching.MatchesIdFilter(t.ProjectId, this.ProjectFilter))
            {
                return false;
            }

            if (!EntityIdMatching.MatchesIdFilter(t.RiskId, this.RiskFilter))
            {
                return false;
            }

            var days = DisplayLabels.DaysSince(t.LastTouchedIso) ?? 0;
            var bucket = days >= StaleDays ? "Stale" : days >= WarnStart ? "Warning" : "Fresh";
            if (this.StalenessFilter != "All" && bucket != this.StalenessFilter)
            {
                return false;
            }

            Duration.ParsedDuration? parsed = Duration.ParseDurationText(t.EstimatedDurationText);
            var mins = parsed?.TotalMinutes;
            if (this.DurationFilter == "Invalid")
            {
                return mins is null;
            }

            if (this.DurationFilter != "All")
            {
                if (mins is null)
                {
                    return false;
                }

                if (this.DurationFilter == "<=30m" && mins > 30)
                {
                    return false;
                }

                if (this.DurationFilter == "<=2h" && mins > 120)
                {
                    return false;
                }

                if (this.DurationFilter == "<=4h" && mins > 240)
                {
                    return false;
                }

                if (this.DurationFilter == "<=1d" && mins > 1440)
                {
                    return false;
                }

                if (this.DurationFilter == ">1d" && mins <= 1440)
                {
                    return false;
                }
            }

            return true;
        }

        private int CompareTasks(AtlasTask a, AtlasTask b, int dir)
        {
            if (this.SortBy == "Priority")
            {
                return (PriorityRank(a.Priority) - PriorityRank(b.Priority)) * dir;
            }

            if (this.SortBy == "Project")
            {
                return EntityIdMatching.CompareLinkedDisplay(
                    a.ProjectId,
                    b.ProjectId,
                    EntityIdMatching.DisplayNameById(a.ProjectId, this._cache.Projects, p => p.Id, p => p.Name),
                    EntityIdMatching.DisplayNameById(b.ProjectId, this._cache.Projects, p => p.Id, p => p.Name),
                    dir);
            }

            if (this.SortBy == "Risk")
            {
                return EntityIdMatching.CompareLinkedDisplay(
                    a.RiskId,
                    b.RiskId,
                    EntityIdMatching.DisplayNameById(a.RiskId, this._cache.Risks, r => r.Id, r => r.Title),
                    EntityIdMatching.DisplayNameById(b.RiskId, this._cache.Risks, r => r.Id, r => r.Title),
                    dir);
            }

            if (this.SortBy == "Title")
            {
                return string.Compare(a.Title, b.Title, StringComparison.Ordinal) * dir;
            }

            if (this.SortBy == "Estimated Duration")
            {
                var am = Duration.ParseDurationText(a.EstimatedDurationText)?.TotalMinutes;
                var bm = Duration.ParseDurationText(b.EstimatedDurationText)?.TotalMinutes;
                if (am is null && bm is null)
                {
                    return 0;
                }

                if (am is null)
                {
                    return 1;
                }

                if (bm is null)
                {
                    return -1;
                }

                return (am.Value - bm.Value) * dir;
            }

            var ad = DisplayLabels.DaysSince(a.LastTouchedIso) ?? 0;
            var bd = DisplayLabels.DaysSince(b.LastTouchedIso) ?? 0;
            var ab = ad >= StaleDays ? 3 : ad >= WarnStart ? 2 : 1;
            var bb = bd >= StaleDays ? 3 : bd >= WarnStart ? 2 : 1;
            if (ab != bb)
            {
                return (ab - bb) * dir;
            }

            return (ad - bd) * dir;
        }

        private static int PriorityRank(Priority p) => p switch
        {
            Priority.Critical => 4,
            Priority.High => 3,
            Priority.Medium => 2,
            _ => 1
        };

        public void Dispose()
        {
            this._cache.Changed -= OnChangedAsync;
            this._selection.Changed -= OnChangedAsync;
            this._ai.RegisterDraftTarget(null);
        }
    }
}
