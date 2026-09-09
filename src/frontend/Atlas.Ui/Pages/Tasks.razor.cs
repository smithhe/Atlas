using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Tasks : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private SelectionState Selection { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private TaskService TaskService { get; set; } = null!;

    [Parameter] public string? TaskId { get; set; }

    private bool _creating;
    private Guid? _autoEditId;

    private string _projectFilter = "All";
    private string _riskFilter = "All";
    private string _statusFilter = "All";
    private string _priorityFilter = "All";
    private string _stalenessFilter = "All";
    private string _durationFilter = "All";
    private string _sortBy = "Priority";
    private string _sortDir = "Desc";

    private bool IsFocusMode => !string.IsNullOrEmpty(TaskId);
    private bool ShowDetail => IsFocusMode || Selection.SelectedTaskId is not null;
    private int StaleDays => Cache.Settings?.StaleDays ?? 10;
    private int WarnStart => Math.Max(1, StaleDays - 2);

    private IReadOnlyList<string> ProjectOptions =>
        Cache.Projects.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

    private IReadOnlyList<string> RiskOptions =>
        Cache.Risks.Select(r => r.Title).OrderBy(t => t, StringComparer.Ordinal).ToList();

    private Dictionary<Guid, TeamMember> MemberById =>
        Cache.Team.ToDictionary(m => m.Id);

    private Dictionary<Guid, AtlasTask> TaskById =>
        Cache.Tasks.ToDictionary(t => t.Id);

    private AtlasTask? Selected
    {
        get
        {
            Guid? id = IsFocusMode && Guid.TryParse(TaskId, out Guid focusId)
                ? focusId
                : Selection.SelectedTaskId;
            if (id is null)
            {
                return null;
            }

            return Cache.Tasks.FirstOrDefault(t => t.Id == id);
        }
    }

    private IReadOnlyList<AtlasTask> FilteredSorted => BuildFilteredSorted();

    protected override void OnInitialized()
    {
        Cache.Changed += OnChanged;
        Selection.Changed += OnChanged;
        Ai.SetContext("Context: Tasks",
        [
            new AiAction("suggest-next-task", "Suggest Next Task"),
            new AiAction("summarize-week", "Summarize Incomplete Work (week)"),
            new AiAction("reprioritize", "Reprioritize suggestions"),
        ]);
        _ = Cache.EnsureHydratedAsync();
    }

    protected override void OnParametersSet()
    {
        if (IsFocusMode && Guid.TryParse(TaskId, out Guid id))
        {
            Selection.SelectTask(id);
            if (Cache.TasksReady && Cache.Tasks.All(t => t.Id != id))
            {
                Nav.NavigateTo("/tasks", replace: true);
            }
        }
    }

    private void OnChanged() => InvokeAsync(() =>
    {
        if (IsFocusMode && Guid.TryParse(TaskId, out Guid id) && Cache.TasksReady && Cache.Tasks.All(t => t.Id != id))
        {
            Nav.NavigateTo("/tasks", replace: true);
            return;
        }

        StateHasChanged();
    });

    private void SelectFromList(Guid id)
    {
        Selection.SelectTask(id);
    }

    private void CloseDetail() => Selection.SelectTask(null);

    private void ClearAutoEdit() => _autoEditId = null;

    private async Task OnTaskDeleted()
    {
        if (IsFocusMode)
        {
            Nav.NavigateTo("/tasks", replace: true);
        }

        if (Selected is not null && _autoEditId == Selected.Id)
        {
            _autoEditId = null;
        }
    }

    private void GoFocus(Guid id) => Nav.NavigateTo($"/tasks/{id}");

    private void EnterFocus()
    {
        if (Selected is null)
        {
            return;
        }

        Nav.NavigateTo($"/tasks/{Selected.Id}");
    }

    private void ExitFocus() => Nav.NavigateTo("/tasks");

    private async Task HandleAddTask()
    {
        if (_creating)
        {
            return;
        }

        _creating = true;
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
            AtlasTask created = await TaskService.CreateAsync(draft);
            Guid id = created.Id;
            _autoEditId = id;
            Selection.SelectTask(id);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to create task right now. Please try again.");
        }
        finally
        {
            _creating = false;
        }
    }

    private void OnProjectFilterChange(ChangeEventArgs e) => _projectFilter = e.Value?.ToString() ?? "All";
    private void OnRiskFilterChange(ChangeEventArgs e) => _riskFilter = e.Value?.ToString() ?? "All";
    private void OnStatusFilterChange(ChangeEventArgs e) => _statusFilter = e.Value?.ToString() ?? "All";
    private void OnPriorityFilterChange(ChangeEventArgs e) => _priorityFilter = e.Value?.ToString() ?? "All";
    private void OnStalenessFilterChange(ChangeEventArgs e) => _stalenessFilter = e.Value?.ToString() ?? "All";
    private void OnDurationFilterChange(ChangeEventArgs e) => _durationFilter = e.Value?.ToString() ?? "All";

    private string SortButtonGlyph(string category) =>
        _sortBy != category ? "↕" : _sortDir == "Asc" ? "▲" : "▼";

    private void ToggleSort(string category)
    {
        if (_sortBy == category)
        {
            _sortDir = _sortDir == "Asc" ? "Desc" : "Asc";
        }
        else
        {
            _sortBy = category;
            _sortDir = category is "Project" or "Risk" or "Title" ? "Asc" : "Desc";
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
        var indexed = Cache.Tasks.Where(PassesFilters).Select((t, i) => (t, i)).ToList();
        var dir = _sortDir == "Asc" ? 1 : -1;
        indexed.Sort((a, b) =>
        {
            var c = CompareTasks(a.t, b.t, dir);
            return c != 0 ? c : a.i.CompareTo(b.i);
        });
        return indexed.Select(x => x.t).ToList();
    }

    private bool PassesFilters(AtlasTask t)
    {
        if (_statusFilter != "All" && DisplayLabels.FormatTaskStatus(t.Status) != _statusFilter)
        {
            return false;
        }

        if (_priorityFilter != "All" && t.Priority.ToString() != _priorityFilter)
        {
            return false;
        }

        if (_projectFilter != "All" && (t.Project ?? "") != _projectFilter)
        {
            return false;
        }

        if (_riskFilter != "All" && (t.Risk ?? "") != _riskFilter)
        {
            return false;
        }

        var days = DisplayLabels.DaysSince(t.LastTouchedIso) ?? 0;
        var bucket = days >= StaleDays ? "Stale" : days >= WarnStart ? "Warning" : "Fresh";
        if (_stalenessFilter != "All" && bucket != _stalenessFilter)
        {
            return false;
        }

        Duration.ParsedDuration? parsed = Duration.ParseDurationText(t.EstimatedDurationText);
        var mins = parsed?.TotalMinutes;
        if (_durationFilter == "Invalid")
        {
            return mins is null;
        }

        if (_durationFilter != "All")
        {
            if (mins is null)
            {
                return false;
            }

            if (_durationFilter == "<=30m" && mins > 30)
            {
                return false;
            }

            if (_durationFilter == "<=2h" && mins > 120)
            {
                return false;
            }

            if (_durationFilter == "<=4h" && mins > 240)
            {
                return false;
            }

            if (_durationFilter == "<=1d" && mins > 1440)
            {
                return false;
            }

            if (_durationFilter == ">1d" && mins <= 1440)
            {
                return false;
            }
        }

        return true;
    }

    private int CompareTasks(AtlasTask a, AtlasTask b, int dir)
    {
        if (_sortBy == "Priority")
        {
            return (PriorityRank(a.Priority) - PriorityRank(b.Priority)) * dir;
        }

        if (_sortBy == "Project")
        {
            return string.Compare(a.Project ?? "", b.Project ?? "", StringComparison.Ordinal) * dir;
        }

        if (_sortBy == "Risk")
        {
            return string.Compare(a.Risk ?? "", b.Risk ?? "", StringComparison.Ordinal) * dir;
        }

        if (_sortBy == "Title")
        {
            return string.Compare(a.Title, b.Title, StringComparison.Ordinal) * dir;
        }

        if (_sortBy == "Estimated Duration")
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
        Cache.Changed -= OnChanged;
        Selection.Changed -= OnChanged;
        Ai.RegisterDraftTarget(null);
    }
}
