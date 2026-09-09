using Microsoft.AspNetCore.Components;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Tasks;

public partial class TaskDetail
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

    private int StaleDays => Cache.Settings?.StaleDays ?? 10;

    private IReadOnlyList<string> ProjectOptions =>
        Cache.Projects.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

    private IReadOnlyList<string> RiskOptions =>
        Cache.Risks.Select(r => r.Title).OrderBy(t => t, StringComparer.Ordinal).ToList();

    private IReadOnlyList<TeamMember> AssigneeOptions =>
        Cache.Team.OrderBy(m => m.Name, StringComparer.Ordinal).ToList();

    private Dictionary<Guid, TeamMember> MemberById =>
        Cache.Team.ToDictionary(m => m.Id);

    private Dictionary<Guid, AtlasTask> TaskById =>
        Cache.Tasks.ToDictionary(t => t.Id);

    protected override void OnParametersSet()
    {
        if (Task is not null)
        {
            _editing = AutoEditId == Task.Id;
            _addBlockerText = "";
        }

        SyncDraftTarget();
    }

    private void ToggleEdit()
    {
        _editing = !_editing;
        if (!_editing && Task is not null && AutoEditId == Task.Id)
        {
            _ = AutoEditCleared.InvokeAsync();
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
            Insert = text =>
            {
                AtlasTask? task = Cache.Tasks.FirstOrDefault(t => t.Id == taskId);
                if (task is null)
                {
                    return;
                }

                var current = task.Notes;
                var next = string.IsNullOrWhiteSpace(current) ? text : $"{current.TrimEnd()}\n\n{text}";
                _ = SaveTaskAsync(EntityClone.Task(task, notes: next));
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

    private void TouchTask() =>
        _ = SaveTaskAsync(EntityClone.Task(Task!, lastTouchedIso: DateTimeOffset.UtcNow.ToString("o")));

    private void OnTitleInput(ChangeEventArgs e) => _ = SaveTaskAsync(EntityClone.Task(Task!, title: e.Value?.ToString() ?? ""));
    private void OnEstimateInput(ChangeEventArgs e) => _ = SaveTaskAsync(EntityClone.Task(Task!, estimatedDurationText: e.Value?.ToString() ?? ""));

    private void OnNotesInput(ChangeEventArgs e) =>
        _ = SaveTaskAsync(EntityClone.Task(Task!, notes: e.Value?.ToString() ?? ""));

    private void OnActualInput(ChangeEventArgs e) =>
        _ = SaveTaskAsync(EntityClone.Task(Task!, actualDurationText: e.Value?.ToString(), setActual: true));

    private void OnStatusChange(ChangeEventArgs e) =>
        _ = SaveTaskAsync(EntityClone.Task(Task!, status: DisplayLabels.ParseTaskStatus(e.Value?.ToString()), setStatus: true));

    private void OnPriorityChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out Priority p))
        {
            _ = SaveTaskAsync(EntityClone.Task(Task!, priority: p));
        }
    }

    private void OnConfidenceChange(ChangeEventArgs e)
    {
        if (Enum.TryParse(e.Value?.ToString(), out Confidence c))
        {
            _ = SaveTaskAsync(EntityClone.Task(Task!, estimateConfidence: c));
        }
    }

    private void OnAssigneeChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        if (string.IsNullOrEmpty(v))
        {
            _ = SaveTaskAsync(EntityClone.Task(Task!, assigneeId: null, setAssignee: true));
            return;
        }

        if (Guid.TryParse(v, out Guid id))
        {
            _ = SaveTaskAsync(EntityClone.Task(Task!, assigneeId: id, setAssignee: true));
        }
    }

    private void OnProjectChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        _ = SaveTaskAsync(EntityClone.Task(Task!, project: string.IsNullOrEmpty(v) ? null : v, setProject: true));
    }

    private void OnRiskChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        _ = SaveTaskAsync(EntityClone.Task(Task!, risk: string.IsNullOrEmpty(v) ? null : v, setRisk: true));
    }

    private void OnDueDateChange(ChangeEventArgs e)
    {
        var v = e.Value?.ToString();
        _ = SaveTaskAsync(EntityClone.Task(Task!, dueDate: string.IsNullOrEmpty(v) ? null : v, setDueDate: true));
    }

    private void OnAddBlockerInput(ChangeEventArgs e) => _addBlockerText = e.Value?.ToString() ?? "";

    private void AddBlocker()
    {
        if (Task is null)
        {
            return;
        }

        IReadOnlyList<AtlasTask> candidates = GetBlockerCandidates(Task);
        Guid? id = ResolveBlockerIdFromInput(_addBlockerText, candidates);
        if (id is null)
        {
            return;
        }

        if (GetTasksThatDependOnMe(Task.Id).Contains(id.Value))
        {
            return;
        }

        IReadOnlyList<Guid> next = Task.DependencyTaskIds.Append(id.Value).Distinct().ToList();
        _ = SaveTaskAsync(EntityClone.Task(Task, dependencyTaskIds: next));
        _addBlockerText = "";
    }

    private void RemoveBlocker(Guid blockerId)
    {
        if (Task is null)
        {
            return;
        }

        IReadOnlyList<Guid> next = Task.DependencyTaskIds.Where(id => id != blockerId).ToList();
        _ = SaveTaskAsync(EntityClone.Task(Task, dependencyTaskIds: next));
    }

    private void ClearDependencies()
    {
        if (Task is null)
        {
            return;
        }

        _ = SaveTaskAsync(EntityClone.Task(Task, dependencyTaskIds: Array.Empty<Guid>()));
    }

    private async Task SaveTaskAsync(AtlasTask next)
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
}
