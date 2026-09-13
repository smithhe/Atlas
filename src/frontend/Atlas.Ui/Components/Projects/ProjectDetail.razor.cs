using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Components.Projects;

public partial class ProjectDetail : IDisposable
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private BrowserDialogs Dialogs { get; set; } = null!;
    [Inject] private ProjectService ProjectService { get; set; } = null!;

    [Parameter, EditorRequired] public Project? Project { get; set; }
    [Parameter] public bool IsFocusMode { get; set; }
    [Parameter] public EventCallback OnEnterFocus { get; set; }
    [Parameter] public EventCallback OnExitFocus { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public Guid? AutoEditId { get; set; }
    [Parameter] public EventCallback AutoEditCleared { get; set; }

    private static readonly HashSet<string> DoneAzureStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "done", "closed", "completed", "resolved", "removed"
    };

    private bool _deleting;
    private bool _editing;
    private bool _saving;
    private Guid? _lastSelectedId;
    private string _tab = "overview";

    private string _draftName = "";
    private string _draftSummary = "";
    private string _draftDescription = "";
    private string _draftStatus = "Active";
    private string _draftHealth = "Green";
    private string _draftTargetDateIso = "";
    private string? _draftPriority;
    private bool _draftPriorityTouched;
    private string _draftProductOwnerId = "";
    private string _tagsText = "";
    private string _draftCheckInDate = "";
    private string _draftCheckInNote = "";
    private List<ProjectLink> _draftLinks = [];

    private string _taskQuery = "";
    private string _taskStatusFilter = "All";
    private string _taskPriorityFilter = "All";
    private string _taskAssigneeFilter = "All";
    private string _taskDueFilter = "All";

    private string _riskQuery = "";
    private string _riskSeverityFilter = "All";
    private string _riskOwnerFilter = "All";

    private string EffectiveTab => _editing ? "overview" : _tab;

    private IReadOnlyList<ProductOwner> SortedProductOwners =>
        Cache.ProductOwners.OrderBy(po => po.Name, StringComparer.OrdinalIgnoreCase).ToList();

    private Dictionary<Guid, TeamMember> MemberById =>
        Cache.Team.ToDictionary(m => m.Id);

    private ProductOwner? ProductOwner =>
        Project?.ProductOwnerId is { } poId
            ? Cache.ProductOwners.FirstOrDefault(po => po.Id == poId)
            : null;

    private IReadOnlyList<TeamMember> TeamMembers =>
        Project is null
            ? Array.Empty<TeamMember>()
            : Cache.Team.Where(m => Project.TeamMemberIds.Contains(m.Id)).ToList();

    private IReadOnlyList<AtlasTask> LinkedTasks =>
        Project is null
            ? Array.Empty<AtlasTask>()
            : Cache.Tasks.Where(t =>
                Project.LinkedTaskIds.Contains(t.Id) ||
                t.ProjectId == Project.Id).ToList();

    private IReadOnlyList<Risk> LinkedRisks =>
        Project is null
            ? Array.Empty<Risk>()
            : Cache.Risks.Where(r =>
                Project.LinkedRiskIds.Contains(r.Id) ||
                r.ProjectId == Project.Id).ToList();

    private IReadOnlyList<AzureItem> LinkedAzureItems
    {
        get
        {
            if (Project is null)
            {
                return Array.Empty<AzureItem>();
            }

            var projectId = Project.Id.ToString();
            var byId = new Dictionary<string, AzureItem>(StringComparer.Ordinal);
            foreach (TeamMember member in Cache.Team)
            {
                foreach (AzureItem item in member.AzureItems)
                {
                    if (item.ProjectId != projectId || byId.ContainsKey(item.Id))
                    {
                        continue;
                    }

                    byId[item.Id] = item;
                }
            }

            return byId.Values.ToList();
        }
    }

    private IReadOnlyList<AtlasTask> FilteredTasks
    {
        get
        {
            var q = _taskQuery.Trim();
            var todayIso = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return LinkedTasks.Where(t =>
            {
                var statusLabel = DisplayLabels.FormatTaskStatus(t.Status);
                if (_taskStatusFilter != "All" && statusLabel != _taskStatusFilter)
                {
                    return false;
                }

                if (_taskPriorityFilter != "All" && t.Priority.ToString() != _taskPriorityFilter)
                {
                    return false;
                }

                if (_taskAssigneeFilter != "All" && (t.AssigneeId?.ToString() ?? "") != _taskAssigneeFilter)
                {
                    return false;
                }

                if (_taskDueFilter != "All" && GetDueBucket(t.DueDate, todayIso) != _taskDueFilter)
                {
                    return false;
                }

                if (q.Length > 0 && !t.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }).ToList();
        }
    }

    private IReadOnlyList<Risk> FilteredRisks
    {
        get
        {
            var q = _riskQuery.Trim();
            return LinkedRisks.Where(r =>
            {
                if (_riskSeverityFilter != "All" && r.Severity != _riskSeverityFilter)
                {
                    return false;
                }

                if (_riskOwnerFilter != "All" && (r.OwnerId?.ToString() ?? "") != _riskOwnerFilter)
                {
                    return false;
                }

                if (q.Length > 0)
                {
                    var hay = $"{r.Title} {r.Description}";
                    if (!hay.Contains(q, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }).ToList();
        }
    }

    private IReadOnlyList<AssigneeOption> TaskAssigneeOptions
    {
        get
        {
            var ids = LinkedTasks
                .Select(t => t.AssigneeId)
                .Where(id => id is not null)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            return ids
                .Select(id => new AssigneeOption(id, MemberById.TryGetValue(id, out TeamMember? m) ? m.Name : id.ToString()))
                .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    private IReadOnlyList<OwnerOption> RiskOwnerOptions
    {
        get
        {
            var ids = LinkedRisks
                .Select(r => r.OwnerId)
                .Where(id => id is not null)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            return ids
                .Select(id => new OwnerOption(id, MemberById.TryGetValue(id, out TeamMember? m) ? m.Name : id.ToString()))
                .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    private ProjectRollup Rollup
    {
        get
        {
            var localTotalTasks = LinkedTasks.Count;
            var localDoneTasks = LinkedTasks.Count(t => (t.Status ?? Models.TaskStatus.NotStarted) == Models.TaskStatus.Done);
            var importedTotalTasks = LinkedAzureItems.Count;
            var importedDoneTasks = LinkedAzureItems.Count(x => IsAzureWorkItemDone(x.Status));
            var totalTasks = localTotalTasks + importedTotalTasks;
            var doneTasks = localDoneTasks + importedDoneTasks;
            var taskCompletionPct = totalTasks > 0 ? (int)Math.Round(doneTasks / (double)totalTasks * 100) : 0;
            var localOpenTasks = LinkedTasks.Count(t => (t.Status ?? Models.TaskStatus.NotStarted) != Models.TaskStatus.Done);
            var importedOpenTasks = LinkedAzureItems.Count(x => !IsAzureWorkItemDone(x.Status));
            var highPriorityOpenTasks = LinkedTasks.Count(t =>
                (t.Status ?? Models.TaskStatus.NotStarted) != Models.TaskStatus.Done &&
                (t.Priority == Priority.High || t.Priority == Priority.Critical));
            var openRisks = LinkedRisks.Count(r => r.Status != RiskStatus.Resolved);
            var atRiskCount = LinkedRisks.Count(r =>
                r.Status != RiskStatus.Resolved && (r.Severity == "High" || r.Severity == "Medium"));

            return new ProjectRollup(
                totalTasks, doneTasks, taskCompletionPct,
                localTotalTasks, localDoneTasks, importedTotalTasks, importedDoneTasks,
                localOpenTasks + importedOpenTasks, localOpenTasks, importedOpenTasks,
                highPriorityOpenTasks, openRisks, atRiskCount);
        }
    }

    protected override void OnInitialized()
    {
        Cache.Changed += OnChangedAsync;
        Nav.LocationChanged += OnLocationChangedAsync;
        SyncTabFromUri();
    }

    protected override void OnParametersSet()
    {
        SyncTabFromUri();

        if (Project is not null)
        {
            var projectChanged = _lastSelectedId != Project.Id;
            if (projectChanged)
            {
                _lastSelectedId = Project.Id;
                var autoEdit = AutoEditId == Project.Id;
                ResetDraftFromProject(Project, autoEdit);
            }
            else if (AutoEditId == Project.Id && !_editing)
            {
                StartEdit();
            }
        }
        else
        {
            _lastSelectedId = null;
        }
    }

    private async void OnLocationChangedAsync(object? sender, LocationChangedEventArgs e)
    {
        try
        {
            await InvokeAsync(() =>
            {
                SyncTabFromUri();
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void SyncTabFromUri()
    {
        if (_editing)
        {
            _tab = "overview";
            return;
        }

        var tab = ReadTabFromUri();
        if (tab != "overview")
        {
            _editing = false;
        }

        _tab = tab;
    }

    private string ReadTabFromUri()
    {
        var raw = GetQueryParam(Nav.Uri, "tab")?.ToLowerInvariant();
        return raw is "tasks" or "risks" ? raw : "overview";
    }

    private static string? GetQueryParam(string uri, string key)
    {
        var qIndex = uri.IndexOf('?', StringComparison.Ordinal);
        if (qIndex < 0)
        {
            return null;
        }

        var query = uri[(qIndex + 1)..];
        var hashIndex = query.IndexOf('#', StringComparison.Ordinal);
        if (hashIndex >= 0)
        {
            query = query[..hashIndex];
        }

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=', StringComparison.Ordinal);
            var name = eq < 0 ? part : part[..eq];
            if (!string.Equals(Uri.UnescapeDataString(name), key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return eq < 0 ? "" : Uri.UnescapeDataString(part[(eq + 1)..]);
        }

        return null;
    }

    private void SetTab(string tab)
    {
        var next = tab is "tasks" or "risks" ? tab : "overview";
        _tab = next;
        if (next != "overview")
        {
            _editing = false;
        }

        var uri = Nav.GetUriWithQueryParameter("tab", next);
        Nav.NavigateTo(uri, forceLoad: false, replace: true);
    }

    private async void OnChangedAsync()
    {
        try
        {
            await InvokeAsync(() =>
            {
                if (Project is not null && AutoEditId == Project.Id && !_editing)
                {
                    StartEdit();
                }

                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void ResetDraftFromProject(Project project, bool autoEdit)
    {
        _editing = autoEdit;
        LoadDraftFrom(project);
    }

    private void LoadDraftFrom(Project project)
    {
        _draftName = project.Name;
        _draftSummary = project.Summary;
        _draftDescription = project.Description ?? "";
        _draftStatus = project.Status?.ToString() ?? "Active";
        _draftHealth = project.Health?.ToString() ?? "Green";
        _draftTargetDateIso = project.TargetDateIso ?? "";
        _draftPriority = project.Priority?.ToString();
        _draftPriorityTouched = false;
        _draftProductOwnerId = project.ProductOwnerId?.ToString() ?? "";
        _tagsText = string.Join(", ", project.Tags);
        _draftCheckInDate = project.LatestCheckIn?.DateIso ?? "";
        _draftCheckInNote = project.LatestCheckIn?.Note ?? "";
        _draftLinks = project.Links.Select(l => new ProjectLink { Label = l.Label, Url = l.Url }).ToList();
    }

    private void StartEdit()
    {
        if (Project is null)
        {
            return;
        }

        if (_tab != "overview")
        {
            SetTab("overview");
        }

        LoadDraftFrom(Project);
        _tab = "overview";
        _editing = true;
        var uri = Nav.GetUriWithQueryParameter("tab", "overview");
        Nav.NavigateTo(uri, forceLoad: false, replace: true);
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

    private void CancelEdit()
    {
        if (Project is not null)
        {
            LoadDraftFrom(Project);
        }

        _editing = false;
        if (Project is not null && AutoEditId == Project.Id)
        {
            ClearAutoEditFireAndForget();
        }
    }

    private void OnDraftTargetDateChange(ChangeEventArgs e) =>
        _draftTargetDateIso = e.Value?.ToString() ?? "";

    private void OnDraftCheckInDateChange(ChangeEventArgs e) =>
        _draftCheckInDate = e.Value?.ToString() ?? "";

    private void OnDraftPriorityChange(ChangeEventArgs e)
    {
        _draftPriority = e.Value?.ToString();
        _draftPriorityTouched = true;
    }

    private void UpdateLinkLabel(int idx, string? value)
    {
        if (idx < 0 || idx >= _draftLinks.Count)
        {
            return;
        }

        _draftLinks[idx].Label = value ?? "";
    }

    private void UpdateLinkUrl(int idx, string? value)
    {
        if (idx < 0 || idx >= _draftLinks.Count)
        {
            return;
        }

        _draftLinks[idx].Url = value ?? "";
    }

    private void RemoveLink(int idx)
    {
        if (idx < 0 || idx >= _draftLinks.Count)
        {
            return;
        }

        _draftLinks.RemoveAt(idx);
    }

    private void AddLink() => _draftLinks.Add(new ProjectLink());

    private Project NormalizeDraftForSave()
    {
        var tags = _tagsText
            .Split(',')
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();
        var links = _draftLinks
            .Select(l => new ProjectLink { Label = l.Label.Trim(), Url = l.Url.Trim() })
            .Where(l => l.Label.Length > 0 && l.Url.Length > 0)
            .ToList();

        var checkInDate = _draftCheckInDate.Trim();
        var checkInNote = _draftCheckInNote.Trim();
        ProjectCheckIn? latestCheckIn = null;
        if (checkInDate.Length > 0 || checkInNote.Length > 0)
        {
            latestCheckIn = new ProjectCheckIn
            {
                DateIso = checkInDate.Length > 0 ? checkInDate : DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Note = checkInNote
            };
        }

        Enum.TryParse(_draftStatus, out ProjectStatus status);
        Enum.TryParse(_draftHealth, out HealthSignal health);
        var persistPriority = Project!.Priority is not null || _draftPriorityTouched;
        Priority? priority = null;
        if (persistPriority && Enum.TryParse(_draftPriority ?? "Medium", out Priority parsedPriority))
        {
            priority = parsedPriority;
        }

        Guid? productOwnerId = string.IsNullOrEmpty(_draftProductOwnerId) ? null : Guid.Parse(_draftProductOwnerId);

        return EntityClone.Project(
            Project!,
            name: string.IsNullOrWhiteSpace(_draftName) ? Project!.Name : _draftName.Trim(),
            summary: _draftSummary,
            description: _draftDescription,
            setDescription: true,
            status: status,
            health: health,
            targetDateIso: string.IsNullOrWhiteSpace(_draftTargetDateIso) ? null : _draftTargetDateIso,
            setTarget: true,
            priority: priority,
            setPriority: persistPriority,
            productOwnerId: productOwnerId,
            setOwner: true,
            tags: tags,
            links: links,
            latestCheckIn: latestCheckIn,
            setLatestCheckIn: true,
            lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o"));
    }

    private async Task SaveOverview()
    {
        if (Project is null || _saving)
        {
            return;
        }

        _saving = true;
        try
        {
            Project next = NormalizeDraftForSave();
            await ProjectService.UpdateAsync(next);
            _editing = false;
            if (AutoEditId == next.Id)
            {
                ClearAutoEditFireAndForget();
            }
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to save project changes right now. Please try again.");
        }
        finally
        {
            _saving = false;
        }
    }

    private string FormatAssignee(Guid? assigneeId)
    {
        if (assigneeId is null)
        {
            return "—";
        }

        return MemberById.TryGetValue(assigneeId.Value, out TeamMember? member) ? member.Name : assigneeId.Value.ToString();
    }

    private static bool IsAzureWorkItemDone(string? status) =>
        !string.IsNullOrWhiteSpace(status) && DoneAzureStatuses.Contains(status.Trim());

    private static string GetDueBucket(string? dueDate, string todayIso)
    {
        if (string.IsNullOrWhiteSpace(dueDate))
        {
            return "No due date";
        }

        if (!DateOnly.TryParse(dueDate.Trim(), out DateOnly due) || !DateOnly.TryParse(todayIso, out DateOnly today))
        {
            return "All";
        }

        if (due < today)
        {
            return "Overdue";
        }

        var days = due.DayNumber - today.DayNumber;
        if (days <= 7)
        {
            return "Next 7 days";
        }

        if (days <= 30)
        {
            return "Next 30 days";
        }

        return "All";
    }

    private void GoTask(Guid id) => Nav.NavigateTo($"/tasks/{id}");
    private void GoRisk(Guid id) => Nav.NavigateTo($"/risks/{id}");

    private async Task HandleDelete()
    {
        if (Project is null || _deleting)
        {
            return;
        }

        Project project = Project;
        if (!await Dialogs.ConfirmAsync($"Delete project \"{project.Name}\"? This cannot be undone."))
        {
            return;
        }

        _deleting = true;
        try
        {
            await ProjectService.DeleteAsync(project.Id);
            await OnDeleted.InvokeAsync();
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to delete this project right now. Please try again.");
        }
        finally
        {
            _deleting = false;
        }
    }

    public void Dispose()
    {
        Cache.Changed -= OnChangedAsync;
        Nav.LocationChanged -= OnLocationChangedAsync;
    }

    private sealed record AssigneeOption(Guid Id, string Name);

    private sealed record OwnerOption(Guid Id, string Name);

    private sealed record ProjectRollup(
        int TotalTasks,
        int DoneTasks,
        int TaskCompletionPct,
        int LocalTotalTasks,
        int LocalDoneTasks,
        int ImportedTotalTasks,
        int ImportedDoneTasks,
        int OpenTasks,
        int LocalOpenTasks,
        int ImportedOpenTasks,
        int HighPriorityOpenTasks,
        int OpenRisks,
        int AtRiskCount);
}
