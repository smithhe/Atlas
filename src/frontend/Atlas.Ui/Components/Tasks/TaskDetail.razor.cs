using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Tasks;

public partial class TaskDetail : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private TaskService TaskService { get; set; } = null!;

    [Parameter] public AtlasTask? Task { get; set; }
    [Parameter] public bool IsFocusMode { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnEnterFocus { get; set; }
    [Parameter] public EventCallback OnExitFocus { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public Guid? AutoEditId { get; set; }
    [Parameter] public EventCallback AutoEditCleared { get; set; }

    private bool _editing;
    private bool _deleting;
    private string _addBlockerText = "";
    private Guid? _trackedTaskId;
    private EntitySaveState _saveState = EntitySaveState.Idle;

    private int StaleDays => Cache.Settings?.StaleDays ?? 10;

    private IReadOnlyList<Project> ProjectOptions =>
        Cache.Projects.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();

    private IReadOnlyList<Risk> RiskOptions =>
        Cache.Risks.OrderBy(r => r.Title, StringComparer.Ordinal).ToList();

    private IReadOnlyList<TeamMember> AssigneeOptions =>
        Cache.Team.OrderBy(m => m.Name, StringComparer.Ordinal).ToList();

    private Dictionary<Guid, TeamMember> MemberById =>
        Cache.Team.ToDictionary(m => m.Id);

    private Dictionary<Guid, AtlasTask> TaskById =>
        Cache.Tasks.ToDictionary(t => t.Id);

    protected override void OnInitialized()
    {
        TaskService.SaveStateChanged += OnSaveStateChangedAsync;
    }

    protected override void OnParametersSet()
    {
        SyncDetailUiForTask(Task?.Id);
        SyncDraftTarget();
    }

    private void SyncDetailUiForTask(Guid? taskId)
    {
        if (_trackedTaskId == taskId)
        {
            if (taskId is not null && AutoEditId == taskId)
            {
                _editing = true;
            }

            return;
        }

        _trackedTaskId = taskId;
        _addBlockerText = "";
        _editing = taskId is not null && AutoEditId == taskId;
        _saveState = taskId is not null ? TaskService.GetSaveState(taskId.Value) : EntitySaveState.Idle;
    }

    private async void OnSaveStateChangedAsync(Guid taskId)
    {
        try
        {
            if (Task?.Id != taskId)
            {
                return;
            }

            await InvokeAsync(() =>
            {
                _saveState = TaskService.GetSaveState(taskId);
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private async void ClearAutoEditFireAndForget()
    {
        try
        {
            await AutoEditCleared.InvokeAsync();
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void ToggleEdit()
    {
        _editing = !_editing;
        if (!_editing && Task is not null && AutoEditId == Task.Id)
        {
            ClearAutoEditFireAndForget();
        }

        SyncDraftTarget();
    }

    private void SyncDraftTarget()
    {
        if (!_editing || Task is null)
        {
            Ai.RegisterDraftTarget(null);
            return;
        }

        Guid taskId = Task.Id;
        Ai.RegisterDraftTarget(new AiDraftTarget
        {
            Label = "task notes",
            Insert = async text =>
            {
                AtlasTask? task = Cache.TryGetTask(taskId);
                if (task is null)
                {
                    return;
                }

                var current = task.Notes;
                var next = string.IsNullOrWhiteSpace(current) ? text : $"{current.TrimEnd()}\n\n{text}";
                await SaveTaskAsync(t => EntityClone.Task(t, notes: next), debounce: true);
            },
        });
    }

    private async Task HandleDelete()
    {
        if (Task is null || _deleting)
        {
            return;
        }

        AtlasTask task = Task;
        if (!await Dialogs.ConfirmAsync($"Delete task \"{task.Title}\"? This cannot be undone."))
        {
            return;
        }

        _deleting = true;
        try
        {
            await TaskService.DeleteAsync(task.Id);
            _trackedTaskId = null;
            await OnDeleted.InvokeAsync();
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

    private async Task TouchTask() =>
        await SaveTaskAsync(t => EntityClone.Task(t, lastTouchedIso: DateTimeOffset.UtcNow.ToString("o")));

    private async Task OnTitleInput(ChangeEventArgs e) =>
        await SaveTaskAsync(t => EntityClone.Task(t, title: e.Value?.ToString() ?? ""), debounce: true);

    private async Task OnEstimateInput(ChangeEventArgs e) =>
        await SaveTaskAsync(t => EntityClone.Task(t, estimatedDurationText: e.Value?.ToString() ?? ""), debounce: true);

    private async Task OnNotesInput(ChangeEventArgs e) =>
        await SaveTaskAsync(t => EntityClone.Task(t, notes: e.Value?.ToString() ?? ""), debounce: true);

    private async Task OnActualInput(ChangeEventArgs e) =>
        await SaveTaskAsync(t => EntityClone.Task(t, actualDurationText: e.Value?.ToString(), setActual: true), debounce: true);

    private async Task OnStatusChange(ChangeEventArgs e) =>
        await SaveTaskAsync(t => EntityClone.Task(t, status: DisplayLabels.ParseTaskStatus(e.Value?.ToString()), setStatus: true));

    private async Task OnPriorityChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out Priority p))
        {
            await SaveTaskAsync(t => EntityClone.Task(t, priority: p));
        }
    }

    private async Task OnConfidenceChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out Confidence c))
        {
            await SaveTaskAsync(t => EntityClone.Task(t, estimateConfidence: c));
        }
    }

    private async Task OnAssigneeChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        if (string.IsNullOrEmpty(v))
        {
            await SaveTaskAsync(t => EntityClone.Task(t, assigneeId: null, setAssignee: true));
            return;
        }

        if (Guid.TryParse(v, out Guid id))
        {
            await SaveTaskAsync(t => EntityClone.Task(t, assigneeId: id, setAssignee: true));
        }
    }

    private async Task OnProjectChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        Guid? projectId = null;
        if (!string.IsNullOrEmpty(v) && Guid.TryParse(v, out Guid parsed))
        {
            projectId = parsed;
        }

        string? projectName = projectId is null ? null : Cache.Projects.FirstOrDefault(p => p.Id == projectId)?.Name;
        await SaveTaskAsync(t => EntityClone.Task(
            t,
            projectId: projectId,
            setProjectId: true,
            project: projectName,
            setProject: true));
    }

    private async Task OnRiskChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        Guid? riskId = null;
        if (!string.IsNullOrEmpty(v) && Guid.TryParse(v, out Guid parsed))
        {
            riskId = parsed;
        }

        string? riskTitle = riskId is null ? null : Cache.Risks.FirstOrDefault(r => r.Id == riskId)?.Title;
        await SaveTaskAsync(t => EntityClone.Task(
            t,
            riskId: riskId,
            setRiskId: true,
            risk: riskTitle,
            setRisk: true));
    }

    private async Task OnDueDateChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        await SaveTaskAsync(t => EntityClone.Task(t, dueDate: string.IsNullOrEmpty(v) ? null : v, setDueDate: true));
    }

    private void OnAddBlockerInput(ChangeEventArgs e) => _addBlockerText = e.Value?.ToString() ?? "";

    private async Task AddBlocker()
    {
        if (Task is null)
        {
            return;
        }

        AtlasTask latest = Cache.TryGetTask(Task.Id) ?? Task;
        IReadOnlyList<AtlasTask> candidates = GetBlockerCandidates(latest);
        Guid? id = ResolveBlockerIdFromInput(_addBlockerText, candidates);
        if (id is null)
        {
            return;
        }

        if (GetTasksThatDependOnMe(latest.Id).Contains(id.Value))
        {
            return;
        }

        IReadOnlyList<Guid> next = latest.DependencyTaskIds.Append(id.Value).Distinct().ToList();
        await SaveTaskAsync(t => EntityClone.Task(t, dependencyTaskIds: next));
        _addBlockerText = "";
    }

    private async Task RemoveBlocker(Guid blockerId)
    {
        if (Task is null)
        {
            return;
        }

        await SaveTaskAsync(t => EntityClone.Task(
            t,
            dependencyTaskIds: t.DependencyTaskIds.Where(id => id != blockerId).ToList()));
    }

    private async Task ClearDependencies()
    {
        if (Task is null)
        {
            return;
        }

        await SaveTaskAsync(t => EntityClone.Task(t, dependencyTaskIds: Array.Empty<Guid>()));
    }

    private async Task SaveTaskAsync(Func<AtlasTask, AtlasTask> edit, bool debounce = false)
    {
        if (Task is null)
        {
            return;
        }

        Guid taskId = Task.Id;
        try
        {
            await TaskService.UpdateAsync(taskId, edit, debounce);
            _saveState = TaskService.GetSaveState(taskId);
        }
        catch (Exception)
        {
            _saveState = TaskService.GetSaveState(taskId);
            await Dialogs.AlertAsync("Unable to save task changes right now. Please try again.");
        }
    }

    private string? AssigneeName(Guid? assigneeId)
    {
        if (assigneeId is null)
        {
            return null;
        }

        return MemberById.TryGetValue(assigneeId.Value, out TeamMember? member)
            ? member.Name
            : assigneeId.Value.ToString();
    }

    private string SaveStateLabel => _saveState switch
    {
        EntitySaveState.Saving => "Saving…",
        EntitySaveState.Saved => "Saved",
        EntitySaveState.Failed => "Save failed",
        EntitySaveState.Idle => "",
        _ => ""
    };

    private IReadOnlyList<AtlasTask> GetVisibleBlockers(AtlasTask task) =>
        task.DependencyTaskIds
            .Select(id => TaskById.GetValueOrDefault(id))
            .Where(t => t is not null && t.Status != Models.TaskStatus.Done)
            .Cast<AtlasTask>()
            .ToList();

    private IReadOnlyList<AtlasTask> GetBlockerCandidates(AtlasTask task)
    {
        var chosen = task.DependencyTaskIds.ToHashSet();
        return Cache.Tasks
            .Where(t => t.Id != task.Id)
            .Where(t => t.Status != Models.TaskStatus.Done)
            .Where(t => !chosen.Contains(t.Id))
            .OrderBy(t => t.Title, StringComparer.Ordinal)
            .ToList();
    }

    private HashSet<Guid> GetTasksThatDependOnMe(Guid taskId)
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
            if (!dependentsById.TryGetValue(cur, out List<Guid>? deps))
            {
                continue;
            }

            foreach (Guid next in deps)
            {
                if (next == taskId)
                {
                    continue;
                }

                if (!seen.Add(next))
                {
                    continue;
                }

                queue.Enqueue(next);
            }
        }

        return seen;
    }

    private static Guid? ResolveBlockerIdFromInput(string inputRaw, IReadOnlyList<AtlasTask> blockerCandidates)
    {
        var input = inputRaw.Trim();
        if (input.Length == 0)
        {
            return null;
        }

        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(input, @"\[([^\]]+)\]\s*$");
        if (m.Success && Guid.TryParse(m.Groups[1].Value.Trim(), out Guid fromBracket))
        {
            return fromBracket;
        }

        AtlasTask? byId = blockerCandidates.FirstOrDefault(t => t.Id.ToString() == input);
        if (byId is not null)
        {
            return byId.Id;
        }

        AtlasTask? exactTitle = blockerCandidates.FirstOrDefault(t => t.Title == input);
        return exactTitle?.Id;
    }

    public void Dispose() => TaskService.SaveStateChanged -= OnSaveStateChangedAsync;
}
