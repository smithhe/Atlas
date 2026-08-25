using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Tasks : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] SelectionState Selection { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;
    [Inject] TaskService TaskService { get; set; } = default!;

    [Parameter] public string? TaskId { get; set; }

    bool _editing;
    bool _creating;
    bool _deleting;
    Guid? _autoEditId;
    string _addBlockerText = "";

    string _projectFilter = "All";
    string _riskFilter = "All";
    string _statusFilter = "All";
    string _priorityFilter = "All";
    string _stalenessFilter = "All";
    string _durationFilter = "All";
    string _sortBy = "Priority";
    string _sortDir = "Desc";

    bool IsFocusMode => !string.IsNullOrEmpty(TaskId);
    bool ShowDetail => IsFocusMode || Selection.SelectedTaskId is not null;
    int StaleDays => Cache.Settings?.StaleDays ?? 10;
    int WarnStart => Math.Max(1, StaleDays - 2);

    IReadOnlyList<string> ProjectOptions =>
        Cache.Projects.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

    IReadOnlyList<string> RiskOptions =>
        Cache.Risks.Select(r => r.Title).OrderBy(t => t, StringComparer.Ordinal).ToList();

    IReadOnlyList<TeamMember> AssigneeOptions =>
        Cache.Team.OrderBy(m => m.Name, StringComparer.Ordinal).ToList();

    Dictionary<Guid, TeamMember> MemberById =>
        Cache.Team.ToDictionary(m => m.Id);

    Dictionary<Guid, AtlasTask> TaskById =>
        Cache.Tasks.ToDictionary(t => t.Id);

    AtlasTask? Selected
    {
        get
        {
            Guid? id = IsFocusMode && Guid.TryParse(TaskId, out Guid focusId)
                ? focusId
                : Selection.SelectedTaskId;
            if (id is null) return null;
            return Cache.Tasks.FirstOrDefault(t => t.Id == id);
        }
    }

    IReadOnlyList<AtlasTask> FilteredSorted => BuildFilteredSorted();

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
            _editing = _autoEditId == id;
            if (Cache.TasksReady && Cache.Tasks.All(t => t.Id != id))
            {
                Nav.NavigateTo("/tasks", replace: true);
            }
        }

        if (Selected is not null)
        {
            _editing = _autoEditId == Selected.Id;
            _addBlockerText = "";
        }

        SyncDraftTarget();
    }

    void OnChanged() => InvokeAsync(() =>
    {
        if (IsFocusMode && Guid.TryParse(TaskId, out Guid id) && Cache.TasksReady && Cache.Tasks.All(t => t.Id != id))
        {
            Nav.NavigateTo("/tasks", replace: true);
            return;
        }

        if (Selected is not null && _autoEditId == Selected.Id)
        {
            _editing = true;
        }

        StateHasChanged();
    });

    void SelectFromList(Guid id)
    {
        Selection.SelectTask(id);
        _editing = _autoEditId == id;
        _addBlockerText = "";
        SyncDraftTarget();
    }

    void ToggleEdit()
    {
        _editing = !_editing;
        if (!_editing && Selected is not null && _autoEditId == Selected.Id)
        {
            _autoEditId = null;
        }

        SyncDraftTarget();
    }

    void SyncDraftTarget()
    {
        if (!_editing || Selected is null)
        {
            Ai.RegisterDraftTarget(null);
            return;
        }

        Guid taskId = Selected.Id;
        Ai.RegisterDraftTarget(new AiDraftTarget
        {
            Label = "task notes",
            Insert = text =>
            {
                AtlasTask? task = Cache.Tasks.FirstOrDefault(t => t.Id == taskId);
                if (task is null) return;
                string current = task.Notes;
                string next = string.IsNullOrWhiteSpace(current) ? text : $"{current.TrimEnd()}\n\n{text}";
                Persist(EntityClone.Task(task, notes: next));
            },
        });
    }

    void GoFocus(Guid id) => Nav.NavigateTo($"/tasks/{id}");

    void EnterFocus()
    {
        if (Selected is null) return;
        Nav.NavigateTo($"/tasks/{Selected.Id}");
    }

    void ExitFocus() => Nav.NavigateTo("/tasks");

    async Task HandleAddTask()
    {
        if (_creating) return;
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
            _editing = true;
            SyncDraftTarget();
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

    async Task HandleDelete()
    {
        if (Selected is null || _deleting) return;
        AtlasTask task = Selected;
        if (!await Dialogs.ConfirmAsync($"Delete task \"{task.Title}\"? This cannot be undone.")) return;

        _deleting = true;
        try
        {
            await TaskService.DeleteAsync(task.Id);
            if (IsFocusMode) Nav.NavigateTo("/tasks", replace: true);
            if (_autoEditId == task.Id) _autoEditId = null;
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to delete this task right now. Please try again.");
        }
        finally
        {
            _deleting = false;
        }
    }

    void TouchTask() =>
        Persist(EntityClone.Task(Selected!, lastTouchedIso: DateTimeOffset.UtcNow.ToString("o")));

    void OnTitleInput(ChangeEventArgs e) => Persist(EntityClone.Task(Selected!, title: e.Value?.ToString() ?? ""));
    void OnEstimateInput(ChangeEventArgs e) => Persist(EntityClone.Task(Selected!, estimatedDurationText: e.Value?.ToString() ?? ""));
    void OnNotesInput(ChangeEventArgs e) =>
        Persist(EntityClone.Task(Selected!, notes: e.Value?.ToString() ?? ""));

    void OnActualInput(ChangeEventArgs e) =>
        Persist(EntityClone.Task(Selected!, actualDurationText: e.Value?.ToString(), setActual: true));

    void OnStatusChange(ChangeEventArgs e) =>
        Persist(EntityClone.Task(Selected!, status: DisplayLabels.ParseTaskStatus(e.Value?.ToString()), setStatus: true));

    void OnPriorityChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out Priority p))
        {
            Persist(EntityClone.Task(Selected!, priority: p));
        }
    }

    void OnConfidenceChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out Confidence c))
        {
            Persist(EntityClone.Task(Selected!, estimateConfidence: c));
        }
    }

    void OnAssigneeChange(ChangeEventArgs e)
    {
        string? v = e.Value?.ToString();
        if (string.IsNullOrEmpty(v))
        {
            Persist(EntityClone.Task(Selected!, assigneeId: null, setAssignee: true));
            return;
        }

        if (Guid.TryParse(v, out Guid id))
        {
            Persist(EntityClone.Task(Selected!, assigneeId: id, setAssignee: true));
        }
    }

    void OnProjectChange(ChangeEventArgs e)
    {
        string? v = e.Value?.ToString();
        Persist(EntityClone.Task(Selected!, project: string.IsNullOrEmpty(v) ? null : v, setProject: true));
    }

    void OnRiskChange(ChangeEventArgs e)
    {
        string? v = e.Value?.ToString();
        Persist(EntityClone.Task(Selected!, risk: string.IsNullOrEmpty(v) ? null : v, setRisk: true));
    }

    void OnDueDateChange(ChangeEventArgs e)
    {
        string? v = e.Value?.ToString();
        Persist(EntityClone.Task(Selected!, dueDate: string.IsNullOrEmpty(v) ? null : v, setDueDate: true));
    }

    void OnAddBlockerInput(ChangeEventArgs e) => _addBlockerText = e.Value?.ToString() ?? "";

    void AddBlocker()
    {
        if (Selected is null) return;
        IReadOnlyList<AtlasTask> candidates = GetBlockerCandidates(Selected);
        Guid? id = ResolveBlockerIdFromInput(_addBlockerText, candidates);
        if (id is null) return;
        if (GetTasksThatDependOnMe(Selected.Id).Contains(id.Value)) return;

        IReadOnlyList<Guid> next = Selected.DependencyTaskIds.Append(id.Value).Distinct().ToList();
        Persist(EntityClone.Task(Selected, dependencyTaskIds: next));
        _addBlockerText = "";
    }

    void RemoveBlocker(Guid blockerId)
    {
        if (Selected is null) return;
        IReadOnlyList<Guid> next = Selected.DependencyTaskIds.Where(id => id != blockerId).ToList();
        Persist(EntityClone.Task(Selected, dependencyTaskIds: next));
    }

    void ClearDependencies()
    {
        if (Selected is null) return;
        Persist(EntityClone.Task(Selected, dependencyTaskIds: Array.Empty<Guid>()));
    }

    void OnProjectFilterChange(ChangeEventArgs e) => _projectFilter = e.Value?.ToString() ?? "All";
    void OnRiskFilterChange(ChangeEventArgs e) => _riskFilter = e.Value?.ToString() ?? "All";
    void OnStatusFilterChange(ChangeEventArgs e) => _statusFilter = e.Value?.ToString() ?? "All";
    void OnPriorityFilterChange(ChangeEventArgs e) => _priorityFilter = e.Value?.ToString() ?? "All";
    void OnStalenessFilterChange(ChangeEventArgs e) => _stalenessFilter = e.Value?.ToString() ?? "All";
    void OnDurationFilterChange(ChangeEventArgs e) => _durationFilter = e.Value?.ToString() ?? "All";

    string SortButtonGlyph(string category) =>
        _sortBy != category ? "↕" : _sortDir == "Asc" ? "▲" : "▼";

    void ToggleSort(string category)
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

    void Persist(AtlasTask next)
    {
        Cache.UpdateTask(next);
        _ = PersistAsync(next);
    }

    async Task PersistAsync(AtlasTask next)
    {
        try
        {
            await TaskService.UpdateAsync(next);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to save task changes right now. Please try again.");
        }
    }

    string ActivityColor(int days) =>
        days >= StaleDays ? "red" : days >= WarnStart ? "yellow" : "green";

    int BlockedByCount(AtlasTask task) =>
        task.DependencyTaskIds.Count(id =>
            TaskById.TryGetValue(id, out AtlasTask? dep) && dep.Status != Models.TaskStatus.Done);

    string? AssigneeName(Guid? assigneeId)
    {
        if (assigneeId is null) return null;
        return MemberById.TryGetValue(assigneeId.Value, out TeamMember? member)
            ? member.Name
            : assigneeId.Value.ToString();
    }

    static string ListDurationPill(string text)
    {
        Duration.ParsedDuration? parsed = Duration.ParseDurationText(text);
        return Duration.FormatDurationFromMinutes(parsed?.TotalMinutes ?? 0);
    }

    IReadOnlyList<AtlasTask> BuildFilteredSorted()
    {
        List<(AtlasTask t, int i)> indexed = Cache.Tasks.Where(PassesFilters).Select((t, i) => (t, i)).ToList();
        int dir = _sortDir == "Asc" ? 1 : -1;
        indexed.Sort((a, b) =>
        {
            int c = CompareTasks(a.t, b.t, dir);
            return c != 0 ? c : a.i.CompareTo(b.i);
        });
        return indexed.Select(x => x.t).ToList();
    }

    bool PassesFilters(AtlasTask t)
    {
        if (_statusFilter != "All" && DisplayLabels.FormatTaskStatus(t.Status) != _statusFilter) return false;
        if (_priorityFilter != "All" && t.Priority.ToString() != _priorityFilter) return false;
        if (_projectFilter != "All" && (t.Project ?? "") != _projectFilter) return false;
        if (_riskFilter != "All" && (t.Risk ?? "") != _riskFilter) return false;

        int days = DisplayLabels.DaysSince(t.LastTouchedIso) ?? 0;
        string bucket = days >= StaleDays ? "Stale" : days >= WarnStart ? "Warning" : "Fresh";
        if (_stalenessFilter != "All" && bucket != _stalenessFilter) return false;

        Duration.ParsedDuration? parsed = Duration.ParseDurationText(t.EstimatedDurationText);
        int? mins = parsed?.TotalMinutes;
        if (_durationFilter == "Invalid") return mins is null;
        if (_durationFilter != "All")
        {
            if (mins is null) return false;
            if (_durationFilter == "<=30m" && mins > 30) return false;
            if (_durationFilter == "<=2h" && mins > 120) return false;
            if (_durationFilter == "<=4h" && mins > 240) return false;
            if (_durationFilter == "<=1d" && mins > 1440) return false;
            if (_durationFilter == ">1d" && mins <= 1440) return false;
        }

        return true;
    }

    int CompareTasks(AtlasTask a, AtlasTask b, int dir)
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
            int? am = Duration.ParseDurationText(a.EstimatedDurationText)?.TotalMinutes;
            int? bm = Duration.ParseDurationText(b.EstimatedDurationText)?.TotalMinutes;
            if (am is null && bm is null) return 0;
            if (am is null) return 1;
            if (bm is null) return -1;
            return (am.Value - bm.Value) * dir;
        }

        int ad = DisplayLabels.DaysSince(a.LastTouchedIso) ?? 0;
        int bd = DisplayLabels.DaysSince(b.LastTouchedIso) ?? 0;
        int ab = ad >= StaleDays ? 3 : ad >= WarnStart ? 2 : 1;
        int bb = bd >= StaleDays ? 3 : bd >= WarnStart ? 2 : 1;
        if (ab != bb) return (ab - bb) * dir;
        return (ad - bd) * dir;
    }

    static int PriorityRank(Priority p) => p switch
    {
        Priority.Critical => 4,
        Priority.High => 3,
        Priority.Medium => 2,
        _ => 1
    };

    IReadOnlyList<AtlasTask> GetVisibleBlockers(AtlasTask task) =>
        task.DependencyTaskIds
            .Select(id => TaskById.GetValueOrDefault(id))
            .Where(t => t is not null && t.Status != Models.TaskStatus.Done)
            .Cast<AtlasTask>()
            .ToList();

    IReadOnlyList<AtlasTask> GetBlockerCandidates(AtlasTask task)
    {
        HashSet<Guid> chosen = task.DependencyTaskIds.ToHashSet();
        return Cache.Tasks
            .Where(t => t.Id != task.Id)
            .Where(t => t.Status != Models.TaskStatus.Done)
            .Where(t => !chosen.Contains(t.Id))
            .OrderBy(t => t.Title, StringComparer.Ordinal)
            .ToList();
    }

    HashSet<Guid> GetTasksThatDependOnMe(Guid taskId)
    {
        var dependentsById = new Dictionary<Guid, List<Guid>>();
        foreach (AtlasTask t in Cache.Tasks)
        {
            foreach (Guid dep in t.DependencyTaskIds)
            {
                if (!dependentsById.TryGetValue(dep, out List<Guid>? arr))
                {
                    arr = new List<Guid>();
                    dependentsById[dep] = arr;
                }

                arr.Add(t.Id);
            }
        }

        var seen = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(taskId);
        while (queue.Count > 0)
        {
            Guid cur = queue.Dequeue();
            if (!dependentsById.TryGetValue(cur, out List<Guid>? deps)) continue;
            foreach (Guid next in deps)
            {
                if (next == taskId) continue;
                if (!seen.Add(next)) continue;
                queue.Enqueue(next);
            }
        }

        return seen;
    }

    static Guid? ResolveBlockerIdFromInput(string inputRaw, IReadOnlyList<AtlasTask> blockerCandidates)
    {
        string input = inputRaw.Trim();
        if (input.Length == 0) return null;

        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(input, @"\[([^\]]+)\]\s*$");
        if (m.Success && Guid.TryParse(m.Groups[1].Value.Trim(), out Guid fromBracket))
        {
            return fromBracket;
        }

        AtlasTask? byId = blockerCandidates.FirstOrDefault(t => t.Id.ToString() == input);
        if (byId is not null) return byId.Id;

        AtlasTask? exactTitle = blockerCandidates.FirstOrDefault(t => t.Title == input);
        return exactTitle?.Id;
    }

    public void Dispose()
    {
        Cache.Changed -= OnChanged;
        Selection.Changed -= OnChanged;
        Ai.RegisterDraftTarget(null);
    }
}
