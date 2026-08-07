using System.Collections.Generic;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>
/// App-wide cache / hydration mirroring React TanStack query topology.
/// Parallel roots: settings, projects, productOwners, team.
/// Chain: projects → risks → tasks.
/// Mutations prefer refetch-after-mutation; optimistic patches keep UI snappy for autosave.
/// </summary>
public sealed class AppCacheService
{
    readonly IAtlasApiClient _api;
    readonly LocalSettings _localSettings;
    readonly SelectionState _selection;
    readonly object _gate = new();

    Task? _hydration;
    bool _defaultAiPanelOpen;

    LoadState _settings = LoadState.Pending;
    LoadState _projects = LoadState.Pending;
    LoadState _productOwners = LoadState.Pending;
    LoadState _team = LoadState.Pending;
    LoadState _risks = LoadState.Pending;
    LoadState _tasks = LoadState.Pending;

    public AppCacheService(IAtlasApiClient api, LocalSettings localSettings, SelectionState selection)
    {
        _api = api;
        _localSettings = localSettings;
        _selection = selection;
    }

    public bool IsHydrating
    {
        get
        {
            lock (_gate)
            {
                return _settings == LoadState.Pending
                    || _projects == LoadState.Pending
                    || _productOwners == LoadState.Pending
                    || _team == LoadState.Pending
                    || _risks == LoadState.Pending
                    || _tasks == LoadState.Pending;
            }
        }
    }

    public bool TasksReady
    {
        get { lock (_gate) return _tasks == LoadState.Ready; }
    }

    public bool RisksReady
    {
        get { lock (_gate) return _risks == LoadState.Ready; }
    }

    public bool ProjectsReady
    {
        get { lock (_gate) return _projects == LoadState.Ready; }
    }

    public bool TeamReady
    {
        get { lock (_gate) return _team == LoadState.Ready; }
    }

    public Settings? Settings { get; private set; }
    public IReadOnlyList<Project> Projects { get; private set; } = Array.Empty<Project>();
    public IReadOnlyList<ProductOwner> ProductOwners { get; private set; } = Array.Empty<ProductOwner>();
    public IReadOnlyList<TeamMember> Team { get; private set; } = Array.Empty<TeamMember>();
    public IReadOnlyList<TeamMemberRisk> TeamMemberRisks { get; private set; } = Array.Empty<TeamMemberRisk>();
    public IReadOnlyList<Risk> Risks { get; private set; } = Array.Empty<Risk>();
    public IReadOnlyList<AtlasTask> Tasks { get; private set; } = Array.Empty<AtlasTask>();

    /// <summary>Per-member growth records keyed by team member id (lazy-loaded).</summary>
    readonly Dictionary<Guid, Growth> _growthByMemberId = new();
    readonly Dictionary<Guid, Task<Growth?>> _growthLoads = new();
    readonly Dictionary<Guid, GrowthLoadStatus> _growthLoadStatus = new();
    readonly Dictionary<Guid, string?> _growthLoadErrors = new();
    readonly Dictionary<Guid, long> _growthLoadGenerations = new();
    readonly Dictionary<Guid, CancellationTokenSource> _growthLoadCancellations = new();

    const string GrowthEnsureFailedMessage = "Unable to ensure growth record for team member.";

    public string? LastError { get; private set; }

    public event Action? Changed;

    public Task EnsureHydratedAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _hydration ??= HydrateAsync(cancellationToken);
            return _hydration;
        }
    }

    async Task HydrateAsync(CancellationToken cancellationToken)
    {
        try
        {
            _defaultAiPanelOpen = await _localSettings.LoadDefaultAiPanelOpenAsync();

            Task settingsTask = LoadSettingsAsync(cancellationToken);
            Task projectsTask = LoadProjectsAsync(cancellationToken);
            Task productOwnersTask = LoadProductOwnersAsync(cancellationToken);
            Task teamTask = LoadTeamAsync(cancellationToken);

            await Task.WhenAll(settingsTask, projectsTask, productOwnersTask, teamTask);
            await LoadRisksAsync(cancellationToken);
            await LoadTasksAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            lock (_gate)
            {
                FailIfPending(ref _settings);
                FailIfPending(ref _projects);
                FailIfPending(ref _productOwners);
                FailIfPending(ref _team);
                FailIfPending(ref _risks);
                FailIfPending(ref _tasks);
            }

            Notify();
        }
    }

    public async Task RefetchTasksAsync(CancellationToken cancellationToken = default)
    {
        await LoadTasksAsync(cancellationToken);
    }

    public async Task RefetchRisksAsync(CancellationToken cancellationToken = default)
    {
        await LoadRisksAsync(cancellationToken);
        await LoadTasksAsync(cancellationToken);
    }

    public async Task RefetchProjectsAsync(CancellationToken cancellationToken = default)
    {
        await LoadProjectsAsync(cancellationToken);
        await LoadRisksAsync(cancellationToken);
        await LoadTasksAsync(cancellationToken);
    }

    public async Task RefetchTeamAsync(CancellationToken cancellationToken = default)
    {
        await LoadTeamAsync(cancellationToken);
    }

    public async Task RefetchProductOwnersAsync(CancellationToken cancellationToken = default)
    {
        await LoadProductOwnersAsync(cancellationToken);
    }

    public async Task RefetchSettingsAsync(CancellationToken cancellationToken = default)
    {
        await LoadSettingsAsync(cancellationToken);
    }

    public void PatchSettings(Settings settings)
    {
        Settings = settings;
        Notify();
    }

    public void AddTask(AtlasTask task)
    {
        Tasks = new[] { task }.Concat(Tasks).ToList();
        SyncProjectLinkedTaskIds(task);
        SyncRiskLinkedTaskIds(task);
        _selection.SelectTask(task.Id);
        Notify();
    }

    public void UpdateTask(AtlasTask task)
    {
        Tasks = Tasks.Select(t => t.Id == task.Id ? task : t).ToList();
        SyncProjectLinkedTaskIds(task);
        SyncRiskLinkedTaskIds(task);
        Notify();
    }

    public void RemoveTask(Guid taskId)
    {
        Tasks = Tasks.Where(t => t.Id != taskId).ToList();
        Projects = Projects.Select(p =>
            p.LinkedTaskIds.Contains(taskId)
                ? CloneProject(p, linkedTaskIds: p.LinkedTaskIds.Where(id => id != taskId).ToList())
                : p).ToList();
        Risks = Risks.Select(r =>
            r.LinkedTaskIds.Contains(taskId)
                ? CloneRisk(r, linkedTaskIds: r.LinkedTaskIds.Where(id => id != taskId).ToList())
                : r).ToList();
        if (_selection.SelectedTaskId == taskId) _selection.SelectTask(null);
        Notify();
    }

    public void AddRisk(Risk risk)
    {
        Risks = new[] { risk }.Concat(Risks).ToList();
        SyncProjectLinkedRiskIds(risk);
        _selection.SelectRisk(risk.Id);
        Notify();
    }

    public void UpdateRisk(Risk risk)
    {
        var previous = Risks.FirstOrDefault(r => r.Id == risk.Id);
        Risks = Risks.Select(r => r.Id == risk.Id ? risk : r).ToList();
        SyncProjectLinkedRiskIds(risk);
        if (previous is not null && previous.Title != risk.Title)
        {
            Tasks = Tasks.Select(t => t.Risk == previous.Title ? CloneTask(t, risk: risk.Title) : t).ToList();
            foreach (var task in Tasks.Where(t => t.Risk == risk.Title))
            {
                SyncRiskLinkedTaskIds(task);
            }
        }

        Notify();
    }

    public void RemoveRisk(Guid riskId)
    {
        var previous = Risks.FirstOrDefault(r => r.Id == riskId);
        Risks = Risks.Where(r => r.Id != riskId).ToList();
        Projects = Projects.Select(p =>
            p.LinkedRiskIds.Contains(riskId)
                ? CloneProject(p, linkedRiskIds: p.LinkedRiskIds.Where(id => id != riskId).ToList())
                : p).ToList();
        if (previous is not null)
        {
            Tasks = Tasks.Select(t => t.Risk == previous.Title ? CloneTask(t, risk: null, clearRisk: true) : t).ToList();
        }

        if (_selection.SelectedRiskId == riskId) _selection.SelectRisk(null);
        Notify();
    }

    public void AddProject(Project project)
    {
        Projects = new[] { project }.Concat(Projects).ToList();
        _selection.SelectProject(project.Id);
        Notify();
    }

    public void UpdateProject(Project project)
    {
        var previous = Projects.FirstOrDefault(p => p.Id == project.Id);
        Projects = Projects.Select(p => p.Id == project.Id ? project : p).ToList();
        if (previous is not null && previous.Name != project.Name)
        {
            Tasks = Tasks.Select(t => t.Project == previous.Name ? CloneTask(t, project: project.Name) : t).ToList();
            Risks = Risks.Select(r => r.Project == previous.Name ? CloneRisk(r, project: project.Name) : r).ToList();
        }

        Notify();
    }

    public void RemoveProject(Guid projectId)
    {
        var previous = Projects.FirstOrDefault(p => p.Id == projectId);
        Projects = Projects.Where(p => p.Id != projectId).ToList();
        if (previous is not null)
        {
            Tasks = Tasks.Select(t => t.Project == previous.Name ? CloneTask(t, project: null, clearProject: true) : t).ToList();
            Risks = Risks.Select(r => r.Project == previous.Name ? CloneRisk(r, project: null, clearProject: true) : r).ToList();
        }

        if (_selection.SelectedProjectId == projectId) _selection.SelectProject(null);
        Notify();
    }

    public void UpdateTeamMember(TeamMember member)
    {
        var next = TeamLogic.WithDerivedActivitySnapshot(member);
        Team = Team.Select(m => m.Id == next.Id ? next : m).ToList();
        Notify();
    }

    public void AddTeamMemberRisk(TeamMemberRisk risk)
    {
        TeamMemberRisks = new[] { risk }.Concat(TeamMemberRisks).ToList();
        Notify();
    }

    public void UpdateTeamMemberRisk(TeamMemberRisk risk)
    {
        TeamMemberRisks = TeamMemberRisks.Select(r => r.Id == risk.Id ? risk : r).ToList();
        Notify();
    }

    public void RemoveTeamMemberRisk(Guid riskId)
    {
        TeamMemberRisks = TeamMemberRisks.Where(r => r.Id != riskId).ToList();
        Notify();
    }

    public Growth? GetGrowth(Guid memberId)
    {
        lock (_gate)
        {
            return _growthByMemberId.TryGetValue(memberId, out var g) ? g : null;
        }
    }

    public GrowthLoadStatus GetGrowthLoadStatus(Guid memberId)
    {
        lock (_gate)
        {
            return _growthLoadStatus.GetValueOrDefault(memberId, GrowthLoadStatus.Idle);
        }
    }

    public string? GetGrowthLoadError(Guid memberId)
    {
        lock (_gate)
        {
            return _growthLoadErrors.TryGetValue(memberId, out var error) ? error : null;
        }
    }

    public void UpdateGrowth(Growth growth)
    {
        lock (_gate)
        {
            _growthByMemberId[growth.MemberId] = growth;
        }

        Notify();
    }

    public Task<Growth?> EnsureGrowthLoadedAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        StartGuardedGrowthLoadAsync(memberId, cancellationToken, LoadGrowthAsync);

    public Task<Growth?> RetryGrowthLoadAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_growthLoadCancellations.TryGetValue(memberId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _growthLoadCancellations.Remove(memberId);
            }

            _growthByMemberId.Remove(memberId);
            _growthLoads.Remove(memberId);
            _growthLoadStatus.Remove(memberId);
            _growthLoadErrors.Remove(memberId);
        }

        return EnsureGrowthLoadedAsync(memberId, cancellationToken);
    }

    bool IsCurrentGrowthLoad(Guid memberId, long generation)
    {
        lock (_gate)
        {
            return _growthLoadGenerations.GetValueOrDefault(memberId) == generation;
        }
    }

    void CompleteGrowthLoad(Guid memberId, long generation)
    {
        lock (_gate)
        {
            if (_growthLoadGenerations.GetValueOrDefault(memberId) != generation)
                return;

            _growthLoads.Remove(memberId);
            if (_growthLoadCancellations.TryGetValue(memberId, out var cts))
            {
                cts.Dispose();
                _growthLoadCancellations.Remove(memberId);
            }
        }
    }

    static bool IsRicherGrowthCache(Growth existing, Growth incoming)
    {
        if (incoming.Goals.Count == 0 && existing.Goals.Count > 0)
            return true;
        if (incoming.SkillsInProgress.Count == 0 && existing.SkillsInProgress.Count > 0)
            return true;
        if (incoming.FeedbackThemes.Count == 0 && existing.FeedbackThemes.Count > 0)
            return true;
        if (string.IsNullOrWhiteSpace(incoming.FocusAreasMarkdown) && !string.IsNullOrWhiteSpace(existing.FocusAreasMarkdown))
            return true;
        return false;
    }

    /// <summary>
    /// Commits growth load results only when <paramref name="generation"/> still owns the member load slot.
    /// Lock owns generation reads/writes; cache/status mutation and Notify happen outside the lock.
    /// When incoming growth is poorer than existing cache, status still advances for the current generation.
    /// </summary>
    bool TryCommitGrowthLoadResult(Guid memberId, long generation, Growth? growth, GrowthLoadStatus status, string? error = null)
    {
        var shouldNotify = false;

        lock (_gate)
        {
            if (_growthLoadGenerations.GetValueOrDefault(memberId) != generation)
                return false;

            if (growth is not null)
            {
                if (!_growthByMemberId.TryGetValue(memberId, out var existing) || !IsRicherGrowthCache(existing, growth))
                    _growthByMemberId[memberId] = growth;
            }

            _growthLoadStatus[memberId] = status;
            if (error is null)
                _growthLoadErrors.Remove(memberId);
            else
                _growthLoadErrors[memberId] = error;

            shouldNotify = true;
        }

        if (shouldNotify)
            Notify();

        return true;
    }

    Task<Growth?> StartGuardedGrowthLoadAsync(Guid memberId, CancellationToken cancellationToken, Func<Guid, long, CancellationToken, Task<Growth?>> loadFactory)
    {
        Task<Growth?> load;
        var startedLoading = false;

        lock (_gate)
        {
            if (_growthByMemberId.TryGetValue(memberId, out var cached))
                return Task.FromResult<Growth?>(cached);

            if (_growthLoads.TryGetValue(memberId, out load!))
                return load;

            var generation = _growthLoadGenerations.GetValueOrDefault(memberId) + 1;
            _growthLoadGenerations[memberId] = generation;
            _growthLoadStatus[memberId] = GrowthLoadStatus.Loading;
            _growthLoadErrors.Remove(memberId);

            if (_growthLoadCancellations.TryGetValue(memberId, out var previousCts))
            {
                previousCts.Cancel();
                previousCts.Dispose();
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _growthLoadCancellations[memberId] = linkedCts;
            load = loadFactory(memberId, generation, linkedCts.Token);
            _growthLoads[memberId] = load;
            startedLoading = true;
        }

        if (startedLoading)
            Notify();

        return load;
    }

    async Task<Growth?> HydrateGrowthAsync(Guid memberId, Guid growthId, long generation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dto = await _api.AtlasApiEndpointsGrowthGetGrowthEndpointAsync(growthId, cancellationToken);
        if (!IsCurrentGrowthLoad(memberId, generation))
            return null;

        var mapped = ApiMappers.MapGrowth(dto);
        TryCommitGrowthLoadResult(memberId, generation, mapped, GrowthLoadStatus.Succeeded);
        return GetGrowth(memberId) ?? mapped;
    }

    async Task<Growth?> LoadGrowthAsync(Guid memberId, long generation, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            AtlasApiDTOsGrowthGrowthDto dto;
            try
            {
                dto = await _api.AtlasApiEndpointsGrowthGetGrowthByTeamMemberEndpointAsync(memberId, cancellationToken);
            }
            catch (AtlasApiException ex) when (ex.StatusCode is 404)
            {
                var ensured = await _api.AtlasApiEndpointsGrowthEnsureGrowthForTeamMemberEndpointAsync(memberId, cancellationToken);
                var growthId = ensured.GrowthId ?? Guid.Empty;
                if (growthId == Guid.Empty)
                {
                    if (!IsCurrentGrowthLoad(memberId, generation))
                        return null;

                    LastError = GrowthEnsureFailedMessage;
                    TryCommitGrowthLoadResult(memberId, generation, null, GrowthLoadStatus.Failed, GrowthEnsureFailedMessage);
                    return null;
                }

                dto = await _api.AtlasApiEndpointsGrowthGetGrowthEndpointAsync(growthId, cancellationToken);
            }

            if (!IsCurrentGrowthLoad(memberId, generation))
                return null;

            var mapped = ApiMappers.MapGrowth(dto);
            TryCommitGrowthLoadResult(memberId, generation, mapped, GrowthLoadStatus.Succeeded);
            return GetGrowth(memberId) ?? mapped;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            if (!IsCurrentGrowthLoad(memberId, generation))
                return null;

            var message = ex is AtlasApiException apiEx
                ? $"Unable to load growth data ({apiEx.StatusCode})."
                : "Unable to load growth data.";
            LastError = message;
            TryCommitGrowthLoadResult(memberId, generation, null, GrowthLoadStatus.Failed, message);
            return null;
        }
        finally
        {
            CompleteGrowthLoad(memberId, generation);
        }
    }

    async Task<Growth?> EnsureGrowthIdHydrateLoadAsync(Guid memberId, long generation, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ensured = await _api.AtlasApiEndpointsGrowthEnsureGrowthForTeamMemberEndpointAsync(memberId, cancellationToken);
            var growthId = ensured.GrowthId ?? Guid.Empty;
            if (growthId == Guid.Empty)
            {
                if (IsCurrentGrowthLoad(memberId, generation))
                {
                    LastError = GrowthEnsureFailedMessage;
                    TryCommitGrowthLoadResult(memberId, generation, null, GrowthLoadStatus.Failed, GrowthEnsureFailedMessage);
                }

                return null;
            }

            return await HydrateGrowthAsync(memberId, growthId, generation, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            if (!IsCurrentGrowthLoad(memberId, generation))
                return null;

            var existing = GetGrowth(memberId);
            if (existing is not null && existing.Id != Guid.Empty)
                return existing;

            var message = ex is AtlasApiException apiEx
                ? $"Unable to load growth data ({apiEx.StatusCode})."
                : "Unable to load growth data.";
            LastError = message;
            TryCommitGrowthLoadResult(memberId, generation, null, GrowthLoadStatus.Failed, message);
            return null;
        }
        finally
        {
            CompleteGrowthLoad(memberId, generation);
        }
    }

    public async Task<Guid> EnsureGrowthIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var cached = GetGrowth(memberId);
        if (cached is not null && cached.Id != Guid.Empty)
            return cached.Id;

        Task<Growth?>? inFlight = null;
        lock (_gate)
        {
            _growthLoads.TryGetValue(memberId, out inFlight);
        }

        if (inFlight is not null)
        {
            var loaded = await inFlight;
            if (loaded is not null && loaded.Id != Guid.Empty)
                return loaded.Id;

            cached = GetGrowth(memberId);
            if (cached is not null && cached.Id != Guid.Empty)
                return cached.Id;
        }
        else if (GetGrowthLoadStatus(memberId) is not GrowthLoadStatus.Failed)
        {
            var loaded = await EnsureGrowthLoadedAsync(memberId, cancellationToken);
            if (loaded is not null && loaded.Id != Guid.Empty)
                return loaded.Id;

            cached = GetGrowth(memberId);
            if (cached is not null && cached.Id != Guid.Empty)
                return cached.Id;
        }

        var hydrateLoad = await StartGuardedGrowthLoadAsync(memberId, cancellationToken, EnsureGrowthIdHydrateLoadAsync);
        if (hydrateLoad is not null && hydrateLoad.Id != Guid.Empty)
            return hydrateLoad.Id;

        cached = GetGrowth(memberId);
        return cached?.Id ?? Guid.Empty;
    }

    public void ReplaceTeamMembers(IReadOnlyList<TeamMember> team, IReadOnlyList<TeamMemberRisk> risks)
    {
        Team = team.Select(TeamLogic.WithDerivedActivitySnapshot).ToList();
        TeamMemberRisks = risks.ToList();
        Notify();
    }

    Guid? FindProjectIdByName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : Projects.FirstOrDefault(p => p.Name == name)?.Id;

    Guid? FindRiskIdByTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? null : Risks.FirstOrDefault(r => r.Title == title)?.Id;

    public AtlasApiDTOsTasksCreateTaskRequest ToCreateTaskRequest(AtlasTask task) =>
        new()
        {
            Title = task.Title,
            Priority = ApiMappers.ToApiPriority(task.Priority),
            Status = ApiMappers.ToApiTaskStatus(task.Status),
            AssigneeId = task.AssigneeId,
            ProjectId = FindProjectIdByName(task.Project),
            RiskId = FindRiskIdByTitle(task.Risk),
            DueDate = ParseDate(task.DueDate),
            DependencyTaskIds = task.DependencyTaskIds.ToList(),
            EstimatedDurationText = task.EstimatedDurationText,
            EstimateConfidence = ApiMappers.ToApiConfidence(task.EstimateConfidence),
            ActualDurationText = task.ActualDurationText,
            Notes = task.Notes
        };

    public AtlasApiDTOsTasksUpdateTaskRequest ToUpdateTaskRequest(AtlasTask task) =>
        new()
        {
            Title = task.Title,
            Priority = ApiMappers.ToApiPriority(task.Priority),
            Status = ApiMappers.ToApiTaskStatus(task.Status),
            AssigneeId = task.AssigneeId,
            ProjectId = FindProjectIdByName(task.Project),
            RiskId = FindRiskIdByTitle(task.Risk),
            DueDate = ParseDate(task.DueDate),
            DependencyTaskIds = task.DependencyTaskIds.ToList(),
            EstimatedDurationText = task.EstimatedDurationText,
            EstimateConfidence = ApiMappers.ToApiConfidence(task.EstimateConfidence),
            ActualDurationText = task.ActualDurationText,
            Notes = task.Notes
        };

    public AtlasApiDTOsRisksCreateRiskRequest ToCreateRiskRequest(Risk risk) =>
        new()
        {
            Title = risk.Title,
            Status = ApiMappers.ToApiRiskStatus(risk.Status),
            Severity = ApiMappers.ToApiSeverity(risk.Severity),
            ProjectId = FindProjectIdByName(risk.Project),
            Description = risk.Description,
            Evidence = risk.Evidence
        };

    public AtlasApiDTOsRisksUpdateRiskRequest ToUpdateRiskRequest(Risk risk) =>
        new()
        {
            Title = risk.Title,
            Status = ApiMappers.ToApiRiskStatus(risk.Status),
            Severity = ApiMappers.ToApiSeverity(risk.Severity),
            ProjectId = FindProjectIdByName(risk.Project),
            Description = risk.Description,
            Evidence = risk.Evidence
        };

    public AtlasApiDTOsProjectsCreateProjectRequest ToCreateProjectRequest(Project project) =>
        new()
        {
            Name = project.Name,
            Summary = project.Summary,
            Description = project.Description,
            Status = ApiMappers.ToApiProjectStatus(project.Status),
            Health = ApiMappers.ToApiHealth(project.Health),
            TargetDate = ParseDate(project.TargetDateIso),
            Priority = project.Priority is null ? null : ApiMappers.ToApiPriority(project.Priority.Value),
            ProductOwnerId = project.ProductOwnerId,
            Tags = project.Tags.ToList(),
            Links = project.Links.Select(l => new AtlasApiDTOsProjectsProjectLinkDto { Label = l.Label, Url = l.Url }).ToList()
        };

    public AtlasApiDTOsProjectsUpdateProjectRequest ToUpdateProjectRequest(Project project) =>
        new()
        {
            Name = project.Name,
            Summary = project.Summary,
            Description = project.Description,
            Status = ApiMappers.ToApiProjectStatus(project.Status),
            Health = ApiMappers.ToApiHealth(project.Health),
            TargetDate = ParseDate(project.TargetDateIso),
            Priority = project.Priority is null ? null : ApiMappers.ToApiPriority(project.Priority.Value),
            ProductOwnerId = project.ProductOwnerId,
            Tags = project.Tags.ToList(),
            Links = project.Links.Select(l => new AtlasApiDTOsProjectsProjectLinkDto { Label = l.Label, Url = l.Url }).ToList()
        };

    static DateTimeOffset? ParseDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;
        return DateTimeOffset.TryParse(iso, out var d) ? d : null;
    }

    void SyncProjectLinkedTaskIds(AtlasTask task)
    {
        Projects = Projects.Select(p =>
        {
            var shouldInclude = !string.IsNullOrEmpty(task.Project) && p.Name == task.Project;
            var has = p.LinkedTaskIds.Contains(task.Id);
            if (shouldInclude && !has)
                return CloneProject(p, linkedTaskIds: p.LinkedTaskIds.Append(task.Id).ToList());
            if (!shouldInclude && has)
                return CloneProject(p, linkedTaskIds: p.LinkedTaskIds.Where(id => id != task.Id).ToList());
            return p;
        }).ToList();
    }

    void SyncRiskLinkedTaskIds(AtlasTask task)
    {
        Risks = Risks.Select(r =>
        {
            var shouldInclude = !string.IsNullOrEmpty(task.Risk) && r.Title == task.Risk;
            var has = r.LinkedTaskIds.Contains(task.Id);
            if (shouldInclude && !has)
                return CloneRisk(r, linkedTaskIds: r.LinkedTaskIds.Append(task.Id).ToList());
            if (!shouldInclude && has)
                return CloneRisk(r, linkedTaskIds: r.LinkedTaskIds.Where(id => id != task.Id).ToList());
            return r;
        }).ToList();
    }

    void SyncProjectLinkedRiskIds(Risk risk)
    {
        Projects = Projects.Select(p =>
        {
            var shouldInclude = !string.IsNullOrEmpty(risk.Project) && p.Name == risk.Project;
            var has = p.LinkedRiskIds.Contains(risk.Id);
            if (shouldInclude && !has)
                return CloneProject(p, linkedRiskIds: p.LinkedRiskIds.Append(risk.Id).ToList());
            if (!shouldInclude && has)
                return CloneProject(p, linkedRiskIds: p.LinkedRiskIds.Where(id => id != risk.Id).ToList());
            return p;
        }).ToList();
    }

    static AtlasTask CloneTask(AtlasTask t, string? project = null, string? risk = null, bool clearProject = false, bool clearRisk = false) =>
        new()
        {
            Id = t.Id,
            Title = t.Title,
            Priority = t.Priority,
            Status = t.Status,
            AssigneeId = t.AssigneeId,
            Project = clearProject ? null : project ?? t.Project,
            Risk = clearRisk ? null : risk ?? t.Risk,
            DueDate = t.DueDate,
            DependencyTaskIds = t.DependencyTaskIds,
            EstimatedDurationText = t.EstimatedDurationText,
            EstimateConfidence = t.EstimateConfidence,
            ActualDurationText = t.ActualDurationText,
            Notes = t.Notes,
            LastTouchedIso = t.LastTouchedIso
        };

    static Project CloneProject(Project p, IReadOnlyList<Guid>? linkedTaskIds = null, IReadOnlyList<Guid>? linkedRiskIds = null) =>
        new()
        {
            Id = p.Id,
            Name = p.Name,
            Summary = p.Summary,
            Description = p.Description,
            Status = p.Status,
            Health = p.Health,
            TargetDateIso = p.TargetDateIso,
            Priority = p.Priority,
            ProductOwnerId = p.ProductOwnerId,
            Tags = p.Tags,
            Links = p.Links,
            LastUpdatedIso = p.LastUpdatedIso,
            LinkedTaskIds = linkedTaskIds ?? p.LinkedTaskIds,
            LinkedRiskIds = linkedRiskIds ?? p.LinkedRiskIds,
            TeamMemberIds = p.TeamMemberIds
        };

    static Risk CloneRisk(Risk r, string? project = null, bool clearProject = false, IReadOnlyList<Guid>? linkedTaskIds = null) =>
        new()
        {
            Id = r.Id,
            Title = r.Title,
            Status = r.Status,
            Severity = r.Severity,
            Project = clearProject ? null : project ?? r.Project,
            OwnerId = r.OwnerId,
            Description = r.Description,
            Evidence = r.Evidence,
            LinkedTaskIds = linkedTaskIds ?? r.LinkedTaskIds,
            LinkedTeamMemberIds = r.LinkedTeamMemberIds,
            History = r.History,
            LastUpdatedIso = r.LastUpdatedIso
        };

    async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            AtlasApiDTOsSettingsSettingsDto dto = await _api.AtlasApiEndpointsSettingsGetSettingsEndpointAsync(cancellationToken);
            Settings = ApiMappers.MapSettings(dto, _defaultAiPanelOpen);
            SetState(ref _settings, LoadState.Ready);
        }
        catch (Exception ex)
        {
            LastError ??= ex.Message;
            SetState(ref _settings, LoadState.Failed);
        }
    }

    async Task LoadProjectsAsync(CancellationToken cancellationToken)
    {
        try
        {
            ICollection<AtlasApiDTOsProjectsProjectDto> dtos = await _api.AtlasApiEndpointsProjectsListProjectsEndpointAsync(cancellationToken: cancellationToken);
            Projects = dtos.Select(ApiMappers.MapProject).ToList();
            SetState(ref _projects, LoadState.Ready);
        }
        catch (Exception ex)
        {
            LastError ??= ex.Message;
            SetState(ref _projects, LoadState.Failed);
            SetState(ref _risks, LoadState.Failed);
            SetState(ref _tasks, LoadState.Failed);
        }
    }

    async Task LoadProductOwnersAsync(CancellationToken cancellationToken)
    {
        try
        {
            ICollection<AtlasApiDTOsProductOwnersProductOwnerListItemDto> dtos = await _api.AtlasApiEndpointsProductOwnersListProductOwnersEndpointAsync(
                ids: null,
                cancellationToken: cancellationToken);
            ProductOwners = dtos.Select(ApiMappers.MapProductOwner).ToList();
            SetState(ref _productOwners, LoadState.Ready);
        }
        catch (Exception ex)
        {
            LastError ??= ex.Message;
            SetState(ref _productOwners, LoadState.Failed);
        }
    }

    async Task LoadTeamAsync(CancellationToken cancellationToken)
    {
        try
        {
            ICollection<AtlasApiDTOsTeamMembersTeamMemberDto> dtos = await _api.AtlasApiEndpointsTeamMembersListTeamMembersEndpointAsync(
                ids: null,
                cancellationToken: cancellationToken);
            List<TeamMember> members = new();
            List<TeamMemberRisk> risks = new();
            foreach (AtlasApiDTOsTeamMembersTeamMemberDto dto in dtos)
            {
                TeamMemberMapResult mapped = ApiMappers.MapTeamMember(dto);
                members.Add(TeamLogic.WithDerivedActivitySnapshot(mapped.Member));
                risks.AddRange(mapped.MemberRisks);
            }

            Team = members;
            TeamMemberRisks = risks;
            SetState(ref _team, LoadState.Ready);
        }
        catch (Exception ex)
        {
            LastError ??= ex.Message;
            SetState(ref _team, LoadState.Failed);
        }
    }

    async Task LoadRisksAsync(CancellationToken cancellationToken)
    {
        if (_projects != LoadState.Ready)
        {
            SetState(ref _risks, LoadState.Failed);
            SetState(ref _tasks, LoadState.Failed);
            return;
        }

        try
        {
            RiskLookups lookups = new()
            {
                ProjectNameById = Projects.ToDictionary(p => p.Id, p => p.Name)
            };
            ICollection<AtlasApiDTOsRisksRiskDto> dtos = await _api.AtlasApiEndpointsRisksListRisksEndpointAsync(cancellationToken: cancellationToken);
            Risks = dtos.Select(d => ApiMappers.MapRisk(d, lookups)).ToList();
            SetState(ref _risks, LoadState.Ready);
        }
        catch (Exception ex)
        {
            LastError ??= ex.Message;
            SetState(ref _risks, LoadState.Failed);
            SetState(ref _tasks, LoadState.Failed);
        }
    }

    async Task LoadTasksAsync(CancellationToken cancellationToken)
    {
        if (_projects != LoadState.Ready || _risks != LoadState.Ready)
        {
            SetState(ref _tasks, LoadState.Failed);
            return;
        }

        try
        {
            TaskLookups lookups = new()
            {
                ProjectNameById = Projects.ToDictionary(p => p.Id, p => p.Name),
                RiskTitleById = Risks.ToDictionary(r => r.Id, r => r.Title)
            };
            ICollection<AtlasApiDTOsTasksTaskDto> dtos = await _api.AtlasApiEndpointsTasksListTasksEndpointAsync(cancellationToken: cancellationToken);
            Tasks = dtos.Select(d => ApiMappers.MapTask(d, lookups)).ToList();
            SetState(ref _tasks, LoadState.Ready);
        }
        catch (Exception ex)
        {
            LastError ??= ex.Message;
            SetState(ref _tasks, LoadState.Failed);
        }
    }

    void SetState(ref LoadState field, LoadState value)
    {
        lock (_gate)
        {
            field = value;
        }

        Notify();
    }

    static void FailIfPending(ref LoadState field)
    {
        if (field == LoadState.Pending)
        {
            field = LoadState.Failed;
        }
    }

    void Notify() => Changed?.Invoke();

    enum LoadState
    {
        Pending,
        Ready,
        Failed
    }
}
