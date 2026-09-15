using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Components.Tasks
{
public partial class TaskDetail : IDisposable
{
    [Inject] private IAppCacheService _cache { get; set; } = null!;
    [Inject] private NavigationManager _nav { get; set; } = null!;
    [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
    [Inject] private IAiStateService _ai { get; set; } = null!;
    [Inject] private ITaskService _taskService { get; set; } = null!;

    [Parameter, EditorRequired] public AtlasTask? Task { get; set; }
    [Parameter] public bool IsFocusMode { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnEnterFocus { get; set; }
    [Parameter] public EventCallback OnExitFocus { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public Guid? AutoEditId { get; set; }
    [Parameter] public EventCallback AutoEditCleared { get; set; }

    private bool Editing { get; set; }
    private bool Deleting { get; set; }
    private string AddBlockerText { get; set; } = "";
    private Guid? TrackedTaskId { get; set; }
    private EntitySaveState SaveState { get; set; } = EntitySaveState.Idle;

    private int StaleDays => this._cache.Settings?.StaleDays ?? 10;

    private IReadOnlyList<Project> ProjectOptions =>
        this._cache.Projects.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();

    private IReadOnlyList<Risk> RiskOptions =>
        this._cache.Risks.OrderBy(r => r.Title, StringComparer.Ordinal).ToList();

    private IReadOnlyList<TeamMember> AssigneeOptions =>
        this._cache.Team.OrderBy(m => m.Name, StringComparer.Ordinal).ToList();

    private Dictionary<Guid, TeamMember> MemberById =>
        this._cache.Team.ToDictionary(m => m.Id);

    private Dictionary<Guid, AtlasTask> TaskById =>
        this._cache.Tasks.ToDictionary(t => t.Id);

    protected override void OnInitialized()
    {
        this._taskService.SaveStateChanged += OnSaveStateChangedAsync;
    }

    protected override void OnParametersSet()
    {
        SyncDetailUiForTask(Task?.Id);
        SyncDraftTarget();
    }

    private void SyncDetailUiForTask(Guid? taskId)
    {
        if (this.TrackedTaskId == taskId)
        {
            if (taskId is not null && AutoEditId == taskId)
            {
                this.Editing = true;
            }

            return;
        }

        this.TrackedTaskId = taskId;
        this.AddBlockerText = "";
        this.Editing = taskId is not null && AutoEditId == taskId;
        this.SaveState = taskId is not null ? this._taskService.GetSaveState(taskId.Value) : EntitySaveState.Idle;
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
                this.SaveState = this._taskService.GetSaveState(taskId);
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
        this.Editing = !this.Editing;
        if (!this.Editing && Task is not null && AutoEditId == Task.Id)
        {
            ClearAutoEditFireAndForget();
        }

        SyncDraftTarget();
    }

    private void SyncDraftTarget()
    {
        if (!this.Editing || Task is null)
        {
            this._ai.RegisterDraftTarget(null);
            return;
        }

        Guid taskId = Task.Id;
        this._ai.RegisterDraftTarget(new AiDraftTarget
        {
            Label = "task notes",
            Insert = async text =>
            {
                AtlasTask? task = this._cache.TryGetTask(taskId);
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
        if (Task is null || this.Deleting)
        {
            return;
        }

        AtlasTask task = Task;
        if (!await this._dialogs.ConfirmAsync($"Delete task \"{task.Title}\"? This cannot be undone."))
        {
            return;
        }

        this.Deleting = true;
        try
        {
            await this._taskService.DeleteAsync(task.Id);
            this.TrackedTaskId = null;
            await OnDeleted.InvokeAsync();
        }
        catch (Exception)
        {
            await this._dialogs.AlertAsync("Unable to delete this task right now. Please try again.");
        }
        finally
        {
            this.Deleting = false;
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

        string? projectName = projectId is null ? null : this._cache.Projects.FirstOrDefault(p => p.Id == projectId)?.Name;
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

        string? riskTitle = riskId is null ? null : this._cache.Risks.FirstOrDefault(r => r.Id == riskId)?.Title;
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

    private void OnAddBlockerInput(ChangeEventArgs e) => this.AddBlockerText = e.Value?.ToString() ?? "";

    private async Task AddBlocker()
    {
        if (Task is null)
        {
            return;
        }

        AtlasTask latest = this._cache.TryGetTask(Task.Id) ?? Task;
        IReadOnlyList<AtlasTask> candidates = GetBlockerCandidates(latest);
        Guid? id = ResolveBlockerIdFromInput(this.AddBlockerText, candidates);
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
        this.AddBlockerText = "";
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
            await this._taskService.UpdateAsync(taskId, edit, debounce);
            this.SaveState = this._taskService.GetSaveState(taskId);
        }
        catch (Exception)
        {
            this.SaveState = this._taskService.GetSaveState(taskId);
            await this._dialogs.AlertAsync("Unable to save task changes right now. Please try again.");
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

    private string SaveStateLabel => this.SaveState switch
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
        return this._cache.Tasks
            .Where(t => t.Id != task.Id)
            .Where(t => t.Status != Models.TaskStatus.Done)
            .Where(t => !chosen.Contains(t.Id))
            .OrderBy(t => t.Title, StringComparer.Ordinal)
            .ToList();
    }

    private HashSet<Guid> GetTasksThatDependOnMe(Guid taskId)
    {
        var dependentsById = new Dictionary<Guid, List<Guid>>();
        foreach (AtlasTask t in this._cache.Tasks)
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

    public void Dispose() => this._taskService.SaveStateChanged -= OnSaveStateChangedAsync;
}
}
