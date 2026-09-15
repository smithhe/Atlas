using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Components.Projects
{
    public partial class ProjectDetail : IDisposable
    {
        [Inject] private IAppCacheService _cache { get; set; } = null!;
        [Inject] private NavigationManager _nav { get; set; } = null!;
        [Inject] private BrowserDialogs _dialogs { get; set; } = null!;
        [Inject] private IProjectService _projectService { get; set; } = null!;

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

        private bool Deleting { get; set; }
        private bool Editing { get; set; }
        private bool Saving { get; set; }
        private Guid? LastSelectedId { get; set; }
        private string Tab { get; set; } = "overview";

        private string DraftName { get; set; } = "";
        private string DraftSummary { get; set; } = "";
        private string DraftDescription { get; set; } = "";
        private string DraftStatus { get; set; } = "Active";
        private string DraftHealth { get; set; } = "Green";
        private string DraftTargetDateIso { get; set; } = "";
        private string? DraftPriority { get; set; }
        private bool DraftPriorityTouched { get; set; }
        private string DraftProductOwnerId { get; set; } = "";
        private string TagsText { get; set; } = "";
        private string DraftCheckInDate { get; set; } = "";
        private string DraftCheckInNote { get; set; } = "";
        private List<ProjectLink> DraftLinks { get; set; } = [];

        private string TaskQuery { get; set; } = "";
        private string TaskStatusFilter { get; set; } = "All";
        private string TaskPriorityFilter { get; set; } = "All";
        private string TaskAssigneeFilter { get; set; } = "All";
        private string TaskDueFilter { get; set; } = "All";

        private string RiskQuery { get; set; } = "";
        private string RiskSeverityFilter { get; set; } = "All";
        private string RiskOwnerFilter { get; set; } = "All";

        private string EffectiveTab => this.Editing ? "overview" : this.Tab;

        private IReadOnlyList<ProductOwner> SortedProductOwners =>
            this._cache.ProductOwners.OrderBy(po => po.Name, StringComparer.OrdinalIgnoreCase).ToList();

        private Dictionary<Guid, TeamMember> MemberById =>
            this._cache.Team.ToDictionary(m => m.Id);

        private ProductOwner? ProductOwner =>
            Project?.ProductOwnerId is { } poId
                ? this._cache.ProductOwners.FirstOrDefault(po => po.Id == poId)
                : null;

        private IReadOnlyList<TeamMember> TeamMembers =>
            Project is null
                ? Array.Empty<TeamMember>()
                : this._cache.Team.Where(m => Project.TeamMemberIds.Contains(m.Id)).ToList();

        private IReadOnlyList<AtlasTask> LinkedTasks =>
            Project is null
                ? Array.Empty<AtlasTask>()
                : this._cache.Tasks.Where(t =>
                    Project.LinkedTaskIds.Contains(t.Id) ||
                    t.ProjectId == Project.Id).ToList();

        private IReadOnlyList<Risk> LinkedRisks =>
            Project is null
                ? Array.Empty<Risk>()
                : this._cache.Risks.Where(r =>
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
                foreach (TeamMember member in this._cache.Team)
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
                var q = this.TaskQuery.Trim();
                var todayIso = DateTime.UtcNow.ToString("yyyy-MM-dd");

                return LinkedTasks.Where(t =>
                {
                    var statusLabel = DisplayLabels.FormatTaskStatus(t.Status);
                    if (this.TaskStatusFilter != "All" && statusLabel != this.TaskStatusFilter)
                    {
                        return false;
                    }

                    if (this.TaskPriorityFilter != "All" && t.Priority.ToString() != this.TaskPriorityFilter)
                    {
                        return false;
                    }

                    if (this.TaskAssigneeFilter != "All" && (t.AssigneeId?.ToString() ?? "") != this.TaskAssigneeFilter)
                    {
                        return false;
                    }

                    if (this.TaskDueFilter != "All" && GetDueBucket(t.DueDate, todayIso) != this.TaskDueFilter)
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
                var q = this.RiskQuery.Trim();
                return LinkedRisks.Where(r =>
                {
                    if (this.RiskSeverityFilter != "All" && r.Severity != this.RiskSeverityFilter)
                    {
                        return false;
                    }

                    if (this.RiskOwnerFilter != "All" && (r.OwnerId?.ToString() ?? "") != this.RiskOwnerFilter)
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
            this._cache.Changed += OnChangedAsync;
            this._nav.LocationChanged += OnLocationChangedAsync;
            SyncTabFromUri();
        }

        protected override void OnParametersSet()
        {
            SyncTabFromUri();

            if (Project is not null)
            {
                var projectChanged = this.LastSelectedId != Project.Id;
                if (projectChanged)
                {
                    this.LastSelectedId = Project.Id;
                    var autoEdit = AutoEditId == Project.Id;
                    ResetDraftFromProject(Project, autoEdit);
                }
                else if (AutoEditId == Project.Id && !this.Editing)
                {
                    StartEdit();
                }
            }
            else
            {
                this.LastSelectedId = null;
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
            if (this.Editing)
            {
                this.Tab = "overview";
                return;
            }

            var tab = ReadTabFromUri();
            if (tab != "overview")
            {
                this.Editing = false;
            }

            this.Tab = tab;
        }

        private string ReadTabFromUri()
        {
            var raw = GetQueryParam(this._nav.Uri, "tab")?.ToLowerInvariant();
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
            this.Tab = next;
            if (next != "overview")
            {
                this.Editing = false;
            }

            var uri = this._nav.GetUriWithQueryParameter("tab", next);
            this._nav.NavigateTo(uri, forceLoad: false, replace: true);
        }

        private async void OnChangedAsync()
        {
            try
            {
                await InvokeAsync(() =>
                {
                    if (Project is not null && AutoEditId == Project.Id && !this.Editing)
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
            this.Editing = autoEdit;
            LoadDraftFrom(project);
        }

        private void LoadDraftFrom(Project project)
        {
            this.DraftName = project.Name;
            this.DraftSummary = project.Summary;
            this.DraftDescription = project.Description ?? "";
            this.DraftStatus = project.Status?.ToString() ?? "Active";
            this.DraftHealth = project.Health?.ToString() ?? "Green";
            this.DraftTargetDateIso = project.TargetDateIso ?? "";
            this.DraftPriority = project.Priority?.ToString();
            this.DraftPriorityTouched = false;
            this.DraftProductOwnerId = project.ProductOwnerId?.ToString() ?? "";
            this.TagsText = string.Join(", ", project.Tags);
            this.DraftCheckInDate = project.LatestCheckIn?.DateIso ?? "";
            this.DraftCheckInNote = project.LatestCheckIn?.Note ?? "";
            this.DraftLinks = project.Links.Select(l => new ProjectLink { Label = l.Label, Url = l.Url }).ToList();
        }

        private void StartEdit()
        {
            if (Project is null)
            {
                return;
            }

            if (this.Tab != "overview")
            {
                SetTab("overview");
            }

            LoadDraftFrom(Project);
            this.Tab = "overview";
            this.Editing = true;
            var uri = this._nav.GetUriWithQueryParameter("tab", "overview");
            this._nav.NavigateTo(uri, forceLoad: false, replace: true);
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

            this.Editing = false;
            if (Project is not null && AutoEditId == Project.Id)
            {
                ClearAutoEditFireAndForget();
            }
        }

        private void OnDraftTargetDateChange(ChangeEventArgs e) =>
            this.DraftTargetDateIso = e.Value?.ToString() ?? "";

        private void OnDraftCheckInDateChange(ChangeEventArgs e) =>
            this.DraftCheckInDate = e.Value?.ToString() ?? "";

        private void OnDraftPriorityChange(ChangeEventArgs e)
        {
            this.DraftPriority = e.Value?.ToString();
            this.DraftPriorityTouched = true;
        }

        private void UpdateLinkLabel(int idx, string? value)
        {
            if (idx < 0 || idx >= this.DraftLinks.Count)
            {
                return;
            }

            this.DraftLinks[idx].Label = value ?? "";
        }

        private void UpdateLinkUrl(int idx, string? value)
        {
            if (idx < 0 || idx >= this.DraftLinks.Count)
            {
                return;
            }

            this.DraftLinks[idx].Url = value ?? "";
        }

        private void RemoveLink(int idx)
        {
            if (idx < 0 || idx >= this.DraftLinks.Count)
            {
                return;
            }

            this.DraftLinks.RemoveAt(idx);
        }

        private void AddLink() => this.DraftLinks.Add(new ProjectLink());

        private Project NormalizeDraftForSave()
        {
            var tags = this.TagsText
                .Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();
            var links = this.DraftLinks
                .Select(l => new ProjectLink { Label = l.Label.Trim(), Url = l.Url.Trim() })
                .Where(l => l.Label.Length > 0 && l.Url.Length > 0)
                .ToList();

            var checkInDate = this.DraftCheckInDate.Trim();
            var checkInNote = this.DraftCheckInNote.Trim();
            ProjectCheckIn? latestCheckIn = null;
            if (checkInDate.Length > 0 || checkInNote.Length > 0)
            {
                latestCheckIn = new ProjectCheckIn
                {
                    DateIso = checkInDate.Length > 0 ? checkInDate : DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    Note = checkInNote
                };
            }

            Enum.TryParse(this.DraftStatus, out ProjectStatus status);
            Enum.TryParse(this.DraftHealth, out HealthSignal health);
            var persistPriority = Project!.Priority is not null || this.DraftPriorityTouched;
            Priority? priority = null;
            if (persistPriority && Enum.TryParse(this.DraftPriority ?? "Medium", out Priority parsedPriority))
            {
                priority = parsedPriority;
            }

            Guid? productOwnerId = string.IsNullOrEmpty(this.DraftProductOwnerId) ? null : Guid.Parse(this.DraftProductOwnerId);

            return EntityClone.Project(
                Project!,
                name: string.IsNullOrWhiteSpace(this.DraftName) ? Project!.Name : this.DraftName.Trim(),
                summary: this.DraftSummary,
                description: this.DraftDescription,
                setDescription: true,
                status: status,
                health: health,
                targetDateIso: string.IsNullOrWhiteSpace(this.DraftTargetDateIso) ? null : this.DraftTargetDateIso,
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
            if (Project is null || this.Saving)
            {
                return;
            }

            this.Saving = true;
            try
            {
                Project next = NormalizeDraftForSave();
                await this._projectService.UpdateAsync(next);
                this.Editing = false;
                if (AutoEditId == next.Id)
                {
                    ClearAutoEditFireAndForget();
                }
            }
            catch (Exception)
            {
                await this._dialogs.AlertAsync("Unable to save project changes right now. Please try again.");
            }
            finally
            {
                this.Saving = false;
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

        private void GoTask(Guid id) => this._nav.NavigateTo($"/tasks/{id}");
        private void GoRisk(Guid id) => this._nav.NavigateTo($"/risks/{id}");

        private async Task HandleDelete()
        {
            if (Project is null || this.Deleting)
            {
                return;
            }

            Project project = Project;
            if (!await this._dialogs.ConfirmAsync($"Delete project \"{project.Name}\"? This cannot be undone."))
            {
                return;
            }

            this.Deleting = true;
            try
            {
                await this._projectService.DeleteAsync(project.Id);
                await OnDeleted.InvokeAsync();
            }
            catch (Exception)
            {
                await this._dialogs.AlertAsync("Unable to delete this project right now. Please try again.");
            }
            finally
            {
                this.Deleting = false;
            }
        }

        public void Dispose()
        {
            this._cache.Changed -= OnChangedAsync;
            this._nav.LocationChanged -= OnLocationChangedAsync;
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
}
