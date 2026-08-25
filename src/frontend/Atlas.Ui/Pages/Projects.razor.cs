using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Pages;

public partial class Projects : IDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] SelectionState Selection { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] BrowserDialogs Dialogs { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;
    [Inject] ProjectService ProjectService { get; set; } = default!;

    [Parameter] public string? ProjectId { get; set; }

    static readonly HashSet<string> DoneAzureStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "done", "closed", "completed", "resolved", "removed"
    };

    bool _creating;
    bool _deleting;
    bool _editing;
    bool _saving;
    Guid? _autoEditId;
    Guid? _lastSelectedId;
    string _tab = "overview";

    string _draftName = "";
    string _draftSummary = "";
    string _draftDescription = "";
    string _draftStatus = "Active";
    string _draftHealth = "Green";
    string _draftTargetDateIso = "";
    string? _draftPriority;
    bool _draftPriorityTouched;
    string _draftProductOwnerId = "";
    string _tagsText = "";
    string _draftCheckInDate = "";
    string _draftCheckInNote = "";
    List<ProjectLink> _draftLinks = [];

    string _taskQuery = "";
    string _taskStatusFilter = "All";
    string _taskPriorityFilter = "All";
    string _taskAssigneeFilter = "All";
    string _taskDueFilter = "All";

    string _riskQuery = "";
    string _riskSeverityFilter = "All";
    string _riskOwnerFilter = "All";

    bool IsFocusMode => !string.IsNullOrEmpty(ProjectId);
    string EffectiveTab => _editing ? "overview" : _tab;

    Project? Selected
    {
        get
        {
            Guid? id = IsFocusMode && Guid.TryParse(ProjectId, out Guid focusId)
                ? focusId
                : Selection.SelectedProjectId;
            if (id is null) return null;
            return Cache.Projects.FirstOrDefault(p => p.Id == id);
        }
    }

    IReadOnlyList<ProductOwner> SortedProductOwners =>
        Cache.ProductOwners.OrderBy(po => po.Name, StringComparer.OrdinalIgnoreCase).ToList();

    Dictionary<Guid, TeamMember> MemberById =>
        Cache.Team.ToDictionary(m => m.Id);

    ProductOwner? ProductOwner =>
        Selected?.ProductOwnerId is { } poId
            ? Cache.ProductOwners.FirstOrDefault(po => po.Id == poId)
            : null;

    IReadOnlyList<TeamMember> TeamMembers =>
        Selected is null
            ? Array.Empty<TeamMember>()
            : Cache.Team.Where(m => Selected.TeamMemberIds.Contains(m.Id)).ToList();

    IReadOnlyList<AtlasTask> LinkedTasks =>
        Selected is null
            ? Array.Empty<AtlasTask>()
            : Cache.Tasks.Where(t =>
                Selected.LinkedTaskIds.Contains(t.Id) ||
                (!string.IsNullOrEmpty(t.Project) && t.Project == Selected.Name)).ToList();

    IReadOnlyList<Risk> LinkedRisks =>
        Selected is null
            ? Array.Empty<Risk>()
            : Cache.Risks.Where(r =>
                Selected.LinkedRiskIds.Contains(r.Id) ||
                (!string.IsNullOrEmpty(r.Project) && r.Project == Selected.Name)).ToList();

    IReadOnlyList<AzureItem> LinkedAzureItems
    {
        get
        {
            if (Selected is null) return Array.Empty<AzureItem>();
            string projectId = Selected.Id.ToString();
            var byId = new Dictionary<string, AzureItem>(StringComparer.Ordinal);
            foreach (TeamMember member in Cache.Team)
            {
                foreach (AzureItem item in member.AzureItems)
                {
                    if (item.ProjectId != projectId || byId.ContainsKey(item.Id)) continue;
                    byId[item.Id] = item;
                }
            }

            return byId.Values.ToList();
        }
    }

    IReadOnlyList<AtlasTask> FilteredTasks
    {
        get
        {
            string q = _taskQuery.Trim();
            string todayIso = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return LinkedTasks.Where(t =>
            {
                string statusLabel = DisplayLabels.FormatTaskStatus(t.Status);
                if (_taskStatusFilter != "All" && statusLabel != _taskStatusFilter) return false;
                if (_taskPriorityFilter != "All" && t.Priority.ToString() != _taskPriorityFilter) return false;
                if (_taskAssigneeFilter != "All" && (t.AssigneeId?.ToString() ?? "") != _taskAssigneeFilter) return false;
                if (_taskDueFilter != "All" && GetDueBucket(t.DueDate, todayIso) != _taskDueFilter) return false;
                if (q.Length > 0 && !t.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }).ToList();
        }
    }

    IReadOnlyList<Risk> FilteredRisks
    {
        get
        {
            string q = _riskQuery.Trim();
            return LinkedRisks.Where(r =>
            {
                if (_riskSeverityFilter != "All" && r.Severity != _riskSeverityFilter) return false;
                if (_riskOwnerFilter != "All" && (r.OwnerId?.ToString() ?? "") != _riskOwnerFilter) return false;
                if (q.Length > 0)
                {
                    string hay = $"{r.Title} {r.Description}";
                    if (!hay.Contains(q, StringComparison.OrdinalIgnoreCase)) return false;
                }

                return true;
            }).ToList();
        }
    }

    IReadOnlyList<AssigneeOption> TaskAssigneeOptions
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

    IReadOnlyList<OwnerOption> RiskOwnerOptions
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

    ProjectRollup Rollup
    {
        get
        {
            int localTotalTasks = LinkedTasks.Count;
            int localDoneTasks = LinkedTasks.Count(t => (t.Status ?? Models.TaskStatus.NotStarted) == Models.TaskStatus.Done);
            int importedTotalTasks = LinkedAzureItems.Count;
            int importedDoneTasks = LinkedAzureItems.Count(x => IsAzureWorkItemDone(x.Status));
            int totalTasks = localTotalTasks + importedTotalTasks;
            int doneTasks = localDoneTasks + importedDoneTasks;
            int taskCompletionPct = totalTasks > 0 ? (int)Math.Round(doneTasks / (double)totalTasks * 100) : 0;
            int localOpenTasks = LinkedTasks.Count(t => (t.Status ?? Models.TaskStatus.NotStarted) != Models.TaskStatus.Done);
            int importedOpenTasks = LinkedAzureItems.Count(x => !IsAzureWorkItemDone(x.Status));
            int highPriorityOpenTasks = LinkedTasks.Count(t =>
                (t.Status ?? Models.TaskStatus.NotStarted) != Models.TaskStatus.Done &&
                (t.Priority == Priority.High || t.Priority == Priority.Critical));
            int openRisks = LinkedRisks.Count(r => r.Status != RiskStatus.Resolved);
            int atRiskCount = LinkedRisks.Count(r =>
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
        Cache.Changed += OnChanged;
        Selection.Changed += OnChanged;
        Nav.LocationChanged += OnLocationChanged;
        Ai.SetContext("Context: Projects",
        [
            new AiAction("project-summary", "Summarize project status"),
            new AiAction("identify-risks", "Identify risks"),
        ]);
        SyncTabFromUri();
        _ = Cache.EnsureHydratedAsync();
    }

    protected override void OnParametersSet()
    {
        SyncTabFromUri();

        if (IsFocusMode && Guid.TryParse(ProjectId, out Guid id))
        {
            Selection.SelectProject(id);
            if (Cache.ProjectsReady && Cache.Projects.All(p => p.Id != id))
            {
                Nav.NavigateTo($"/projects{CurrentSearch}", replace: true);
            }
        }

        if (Selected is not null)
        {
            bool projectChanged = _lastSelectedId != Selected.Id;
            if (projectChanged)
            {
                _lastSelectedId = Selected.Id;
                bool autoEdit = _autoEditId == Selected.Id;
                ResetDraftFromProject(Selected, autoEdit);
            }
            else if (_autoEditId == Selected.Id && !_editing)
            {
                StartEdit();
            }
        }
    }

    void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(() =>
        {
            SyncTabFromUri();
            StateHasChanged();
        });

    void SyncTabFromUri()
    {
        if (_editing)
        {
            _tab = "overview";
            return;
        }

        string tab = ReadTabFromUri();
        if (tab != "overview")
        {
            _editing = false;
        }

        _tab = tab;
    }

    string ReadTabFromUri()
    {
        string? raw = GetQueryParam(Nav.Uri, "tab")?.ToLowerInvariant();
        return raw is "tasks" or "risks" ? raw : "overview";
    }

    static string? GetQueryParam(string uri, string key)
    {
        int qIndex = uri.IndexOf('?', StringComparison.Ordinal);
        if (qIndex < 0) return null;
        string query = uri[(qIndex + 1)..];
        int hashIndex = query.IndexOf('#', StringComparison.Ordinal);
        if (hashIndex >= 0) query = query[..hashIndex];
        foreach (string part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = part.IndexOf('=', StringComparison.Ordinal);
            string name = eq < 0 ? part : part[..eq];
            if (!string.Equals(Uri.UnescapeDataString(name), key, StringComparison.OrdinalIgnoreCase))
                continue;
            return eq < 0 ? "" : Uri.UnescapeDataString(part[(eq + 1)..]);
        }

        return null;
    }

    void SetTab(string tab)
    {
        string next = tab is "tasks" or "risks" ? tab : "overview";
        _tab = next;
        if (next != "overview")
        {
            _editing = false;
        }

        string uri = Nav.GetUriWithQueryParameter("tab", next);
        Nav.NavigateTo(uri, forceLoad: false, replace: true);
    }

    void OnChanged() => InvokeAsync(() =>
    {
        if (IsFocusMode && Guid.TryParse(ProjectId, out Guid id) && Cache.ProjectsReady && Cache.Projects.All(p => p.Id != id))
        {
            Nav.NavigateTo($"/projects{CurrentSearch}", replace: true);
            return;
        }

        if (Selected is not null && _autoEditId == Selected.Id && !_editing)
        {
            StartEdit();
        }

        StateHasChanged();
    });

    void SelectFromList(Guid id)
    {
        Selection.SelectProject(id);
        if (_autoEditId == id)
        {
            StartEdit();
        }
        else
        {
            _editing = false;
        }

        StateHasChanged();
    }

    void ResetDraftFromProject(Project project, bool autoEdit)
    {
        _editing = autoEdit;
        LoadDraftFrom(project);
    }

    void LoadDraftFrom(Project project)
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

    void StartEdit()
    {
        if (Selected is null) return;
        if (_tab != "overview")
        {
            SetTab("overview");
        }

        LoadDraftFrom(Selected);
        _tab = "overview";
        _editing = true;
        string uri = Nav.GetUriWithQueryParameter("tab", "overview");
        Nav.NavigateTo(uri, forceLoad: false, replace: true);
    }

    void CancelEdit()
    {
        if (Selected is not null)
        {
            LoadDraftFrom(Selected);
        }

        _editing = false;
        if (Selected is not null && _autoEditId == Selected.Id) _autoEditId = null;
    }

    void OnDraftTargetDateChange(ChangeEventArgs e) =>
        _draftTargetDateIso = e.Value?.ToString() ?? "";

    void OnDraftCheckInDateChange(ChangeEventArgs e) =>
        _draftCheckInDate = e.Value?.ToString() ?? "";

    void OnDraftPriorityChange(ChangeEventArgs e)
    {
        _draftPriority = e.Value?.ToString();
        _draftPriorityTouched = true;
    }

    void UpdateLinkLabel(int idx, string? value)
    {
        if (idx < 0 || idx >= _draftLinks.Count) return;
        _draftLinks[idx].Label = value ?? "";
    }

    void UpdateLinkUrl(int idx, string? value)
    {
        if (idx < 0 || idx >= _draftLinks.Count) return;
        _draftLinks[idx].Url = value ?? "";
    }

    void RemoveLink(int idx)
    {
        if (idx < 0 || idx >= _draftLinks.Count) return;
        _draftLinks.RemoveAt(idx);
    }

    void AddLink() => _draftLinks.Add(new ProjectLink());

    Project NormalizeDraftForSave()
    {
        List<string> tags = _tagsText
            .Split(',')
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();
        List<ProjectLink> links = _draftLinks
            .Select(l => new ProjectLink { Label = l.Label.Trim(), Url = l.Url.Trim() })
            .Where(l => l.Label.Length > 0 && l.Url.Length > 0)
            .ToList();

        string checkInDate = _draftCheckInDate.Trim();
        string checkInNote = _draftCheckInNote.Trim();
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
        bool persistPriority = Selected!.Priority is not null || _draftPriorityTouched;
        Priority? priority = null;
        if (persistPriority && Enum.TryParse(_draftPriority ?? "Medium", out Priority parsedPriority))
        {
            priority = parsedPriority;
        }

        Guid? productOwnerId = string.IsNullOrEmpty(_draftProductOwnerId) ? null : Guid.Parse(_draftProductOwnerId);

        return EntityClone.Project(
            Selected!,
            name: string.IsNullOrWhiteSpace(_draftName) ? Selected!.Name : _draftName.Trim(),
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

    async Task SaveOverview()
    {
        if (Selected is null || _saving) return;
        _saving = true;
        try
        {
            Project next = NormalizeDraftForSave();
            await ProjectService.UpdateAsync(next);
            Cache.UpdateProject(next);
            _editing = false;
            if (_autoEditId == next.Id) _autoEditId = null;
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

    string FormatAssignee(Guid? assigneeId)
    {
        if (assigneeId is null) return "—";
        return MemberById.TryGetValue(assigneeId.Value, out TeamMember? member) ? member.Name : assigneeId.Value.ToString();
    }

    static bool IsAzureWorkItemDone(string? status) =>
        !string.IsNullOrWhiteSpace(status) && DoneAzureStatuses.Contains(status.Trim());

    static string GetDueBucket(string? dueDate, string todayIso)
    {
        if (string.IsNullOrWhiteSpace(dueDate)) return "No due date";
        if (!DateOnly.TryParse(dueDate.Trim(), out DateOnly due) || !DateOnly.TryParse(todayIso, out DateOnly today))
            return "All";
        if (due < today) return "Overdue";
        int days = due.DayNumber - today.DayNumber;
        if (days <= 7) return "Next 7 days";
        if (days <= 30) return "Next 30 days";
        return "All";
    }

    void GoFocus(Guid id) => Nav.NavigateTo($"/projects/{id}");
    void GoTask(Guid id) => Nav.NavigateTo($"/tasks/{id}");
    void GoRisk(Guid id) => Nav.NavigateTo($"/risks/{id}");

    string CurrentSearch
    {
        get
        {
            string uri = Nav.Uri;
            int qIndex = uri.IndexOf('?', StringComparison.Ordinal);
            if (qIndex < 0) return "";
            string query = uri[qIndex..];
            int hashIndex = query.IndexOf('#', StringComparison.Ordinal);
            return hashIndex >= 0 ? query[..hashIndex] : query;
        }
    }

    void EnterFocus()
    {
        if (Selected is null) return;
        Nav.NavigateTo($"/projects/{Selected.Id}{CurrentSearch}");
    }

    void ExitFocus() => Nav.NavigateTo($"/projects{CurrentSearch}");

    async Task HandleAddProject()
    {
        if (_creating) return;
        string? requested = await Dialogs.PromptAsync("Project name", "New project");
        if (requested is null) return;
        string name = string.IsNullOrWhiteSpace(requested) ? "New project" : requested.Trim();
        _creating = true;
        try
        {
            var draft = new Project
            {
                Name = name,
                Summary = "New project summary",
                Description = "",
                Status = ProjectStatus.Active,
                Health = HealthSignal.Green,
                Tags = Array.Empty<string>(),
                Links = Array.Empty<ProjectLink>(),
                LinkedTaskIds = Array.Empty<Guid>(),
                LinkedRiskIds = Array.Empty<Guid>(),
                TeamMemberIds = Array.Empty<Guid>(),
                LastUpdatedIso = DateTimeOffset.UtcNow.ToString("o")
            };
            Project created = await ProjectService.CreateAsync(draft);
        Guid id = created.Id;
            _autoEditId = id;
            SelectFromList(id);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Unable to create project right now. Please try again.");
        }
        finally
        {
            _creating = false;
        }
    }

    async Task HandleDelete()
    {
        if (Selected is null || _deleting) return;
        Project project = Selected;
        if (!await Dialogs.ConfirmAsync($"Delete project \"{project.Name}\"? This cannot be undone.")) return;

        _deleting = true;
        try
        {
            await ProjectService.DeleteAsync(project.Id);
            if (IsFocusMode) Nav.NavigateTo($"/projects{CurrentSearch}", replace: true);
            if (_autoEditId == project.Id) _autoEditId = null;
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
        Cache.Changed -= OnChanged;
        Selection.Changed -= OnChanged;
        Nav.LocationChanged -= OnLocationChanged;
    }

    sealed record AssigneeOption(Guid Id, string Name);
    sealed record OwnerOption(Guid Id, string Name);
    sealed record ProjectRollup(
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
