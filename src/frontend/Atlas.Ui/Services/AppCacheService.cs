using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{
    /// <summary>
    /// App-wide cache / hydration mirroring React TanStack query topology.
    /// Parallel roots: settings, projects, productOwners, team.
    /// Chain: projects → risks → tasks.
    /// Mutations prefer refetch-after-mutation; optimistic patches keep UI snappy for autosave.
    /// </summary>
    public sealed class AppCacheService : IAppCacheService
    {
        private readonly IAtlasApiClient _api;
        private readonly LocalSettings _localSettings;
        private readonly SelectionState _selection;
        private readonly object _gate = new();

        private Task? _hydration;
        private bool _defaultAiPanelOpen;

        private LoadState _settings = LoadState.Pending;
        private LoadState _projects = LoadState.Pending;
        private LoadState _productOwners = LoadState.Pending;
        private LoadState _team = LoadState.Pending;
        private LoadState _risks = LoadState.Pending;
        private LoadState _tasks = LoadState.Pending;

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
            get { lock (_gate)
                {
                    return _tasks == LoadState.Ready;
                }
            }
        }

        public bool RisksReady
        {
            get { lock (_gate)
                {
                    return _risks == LoadState.Ready;
                }
            }
        }

        public bool ProjectsReady
        {
            get { lock (_gate)
                {
                    return _projects == LoadState.Ready;
                }
            }
        }

        public bool TeamReady
        {
            get { lock (_gate)
                {
                    return _team == LoadState.Ready;
                }
            }
        }

        public Settings? Settings { get; private set; }
        public IReadOnlyList<Project> Projects { get; private set; } = Array.Empty<Project>();
        public IReadOnlyList<ProductOwner> ProductOwners { get; private set; } = Array.Empty<ProductOwner>();
        public IReadOnlyList<TeamMember> Team { get; private set; } = Array.Empty<TeamMember>();
        public IReadOnlyList<TeamMemberRisk> TeamMemberRisks { get; private set; } = Array.Empty<TeamMemberRisk>();
        public IReadOnlyList<Risk> Risks { get; private set; } = Array.Empty<Risk>();
        public IReadOnlyList<AtlasTask> Tasks { get; private set; } = Array.Empty<AtlasTask>();

        /// <summary>Per-member growth records keyed by team member id (lazy-loaded).</summary>
        private readonly Dictionary<Guid, Growth> _growthByMemberId = new();

        private readonly Dictionary<Guid, Task<Growth?>> _growthLoads = new();
        private readonly Dictionary<Guid, GrowthLoadStatus> _growthLoadStatus = new();
        private readonly Dictionary<Guid, string?> _growthLoadErrors = new();
        private readonly Dictionary<Guid, long> _growthLoadGenerations = new();
        private readonly Dictionary<Guid, CancellationTokenSource> _growthLoadCancellations = new();

        private const string GrowthEnsureFailedMessage = "Unable to ensure growth record for team member.";
        private const string HydrationFailedMessage = "Unable to load application data.";

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

        public void RecordHydrationFailure(Exception ex)
        {
            LastError ??= HydrationFailedMessage;
            Notify();
        }

        private async Task HydrateAsync(CancellationToken cancellationToken)
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

        public AtlasTask? TryGetTask(Guid taskId) => Tasks.FirstOrDefault(t => t.Id == taskId);

        public void RemoveTask(Guid taskId)
        {
            Tasks = Tasks.Where(t => t.Id != taskId).ToList();
            Projects = Projects.Select(p =>
                p.LinkedTaskIds.Contains(taskId)
                    ? EntityClone.Project(p, linkedTaskIds: p.LinkedTaskIds.Where(id => id != taskId).ToList())
                    : p).ToList();
            Risks = Risks.Select(r =>
                r.LinkedTaskIds.Contains(taskId)
                    ? EntityClone.Risk(r, linkedTaskIds: r.LinkedTaskIds.Where(id => id != taskId).ToList())
                    : r).ToList();
            if (_selection.SelectedTaskId == taskId)
            {
                _selection.SelectTask(null);
            }

            Notify();
        }

        public void AddRisk(Risk risk)
        {
            Risks = new[] { risk }.Concat(Risks).ToList();
            SyncProjectLinkedRiskIds(risk);
            _selection.SelectRisk(risk.Id);
            Notify();
        }

        public Risk? TryGetRisk(Guid riskId) => Risks.FirstOrDefault(r => r.Id == riskId);

        public void UpdateRisk(Risk risk)
        {
            Risk? previous = Risks.FirstOrDefault(r => r.Id == risk.Id);
            Risks = Risks.Select(r => r.Id == risk.Id ? risk : r).ToList();
            SyncProjectLinkedRiskIds(risk);
            if (previous is not null && previous.Title != risk.Title)
            {
                Tasks = Tasks.Select(t => t.RiskId == risk.Id ? EntityClone.Task(t, risk: risk.Title, setRisk: true) : t).ToList();
                foreach (AtlasTask task in Tasks.Where(t => t.RiskId == risk.Id))
                {
                    SyncRiskLinkedTaskIds(task);
                }
            }

            Notify();
        }

        public void RemoveRisk(Guid riskId)
        {
            Risk? previous = Risks.FirstOrDefault(r => r.Id == riskId);
            Risks = Risks.Where(r => r.Id != riskId).ToList();
            Projects = Projects.Select(p =>
                p.LinkedRiskIds.Contains(riskId)
                    ? EntityClone.Project(p, linkedRiskIds: p.LinkedRiskIds.Where(id => id != riskId).ToList())
                    : p).ToList();
            if (previous is not null)
            {
                Tasks = Tasks.Select(t => t.RiskId == riskId ? EntityClone.Task(t, riskId: null, setRiskId: true, risk: null, setRisk: true) : t).ToList();
            }

            if (_selection.SelectedRiskId == riskId)
            {
                _selection.SelectRisk(null);
            }

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
            Project? previous = Projects.FirstOrDefault(p => p.Id == project.Id);
            Projects = Projects.Select(p => p.Id == project.Id ? project : p).ToList();
            if (previous is not null && previous.Name != project.Name)
            {
                Tasks = Tasks.Select(t => t.ProjectId == project.Id ? EntityClone.Task(t, project: project.Name, setProject: true) : t).ToList();
                Risks = Risks.Select(r => r.ProjectId == project.Id ? EntityClone.Risk(r, project: project.Name, setProject: true) : r).ToList();
            }

            Notify();
        }

        public void RemoveProject(Guid projectId)
        {
            Project? previous = Projects.FirstOrDefault(p => p.Id == projectId);
            Projects = Projects.Where(p => p.Id != projectId).ToList();
            if (previous is not null)
            {
                Tasks = Tasks.Select(t => t.ProjectId == projectId ? EntityClone.Task(t, projectId: null, setProjectId: true, project: null, setProject: true) : t).ToList();
                Risks = Risks.Select(r => r.ProjectId == projectId ? EntityClone.Risk(r, projectId: null, setProjectId: true, project: null, setProject: true) : r).ToList();
            }

            if (_selection.SelectedProjectId == projectId)
            {
                _selection.SelectProject(null);
            }

            Notify();
        }

        public void UpdateTeamMember(TeamMember member)
        {
            TeamMember next = TeamLogic.WithDerivedActivitySnapshot(member);
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
                return _growthByMemberId.TryGetValue(memberId, out Growth? g) ? g : null;
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
                if (_growthLoadCancellations.TryGetValue(memberId, out CancellationTokenSource? cts))
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

        private bool IsCurrentGrowthLoad(Guid memberId, long generation)
        {
            lock (_gate)
            {
                return _growthLoadGenerations.GetValueOrDefault(memberId) == generation;
            }
        }

        private void CompleteGrowthLoad(Guid memberId, long generation)
        {
            lock (_gate)
            {
                if (_growthLoadGenerations.GetValueOrDefault(memberId) != generation)
                {
                    return;
                }

                _growthLoads.Remove(memberId);
                if (_growthLoadCancellations.TryGetValue(memberId, out CancellationTokenSource? cts))
                {
                    cts.Dispose();
                    _growthLoadCancellations.Remove(memberId);
                }
            }
        }

        private static bool IsRicherGrowthCache(Growth existing, Growth incoming)
        {
            if (incoming.Goals.Count == 0 && existing.Goals.Count > 0)
            {
                return true;
            }

            if (incoming.SkillsInProgress.Count == 0 && existing.SkillsInProgress.Count > 0)
            {
                return true;
            }

            if (incoming.FeedbackThemes.Count == 0 && existing.FeedbackThemes.Count > 0)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(incoming.FocusAreasMarkdown) && !string.IsNullOrWhiteSpace(existing.FocusAreasMarkdown))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Commits growth load results only when <paramref name="generation"/> still owns the member load slot.
        /// Lock owns generation reads/writes; cache/status mutation and Notify happen outside the lock.
        /// When incoming growth is poorer than existing cache, status still advances for the current generation.
        /// </summary>
        private bool TryCommitGrowthLoadResult(Guid memberId, long generation, Growth? growth, GrowthLoadStatus status, string? error = null)
        {
            bool shouldNotify;

            lock (_gate)
            {
                if (_growthLoadGenerations.GetValueOrDefault(memberId) != generation)
                {
                    return false;
                }

                if (growth is not null)
                {
                    if (!_growthByMemberId.TryGetValue(memberId, out Growth? existing) || !IsRicherGrowthCache(existing, growth))
                    {
                        _growthByMemberId[memberId] = growth;
                    }
                }

                _growthLoadStatus[memberId] = status;
                if (error is null)
                {
                    _growthLoadErrors.Remove(memberId);
                }
                else
                {
                    _growthLoadErrors[memberId] = error;
                }

                shouldNotify = true;
            }

            if (shouldNotify)
            {
                Notify();
            }

            return true;
        }

        private Task<Growth?> StartGuardedGrowthLoadAsync(Guid memberId, CancellationToken cancellationToken, Func<Guid, long, CancellationToken, Task<Growth?>> loadFactory)
        {
            Task<Growth?> load;
            bool startedLoading;

            lock (_gate)
            {
                if (_growthByMemberId.TryGetValue(memberId, out Growth? cached))
                {
                    return Task.FromResult<Growth?>(cached);
                }

                if (_growthLoads.TryGetValue(memberId, out load!))
                {
                    return load;
                }

                var generation = _growthLoadGenerations.GetValueOrDefault(memberId) + 1;
                _growthLoadGenerations[memberId] = generation;
                _growthLoadStatus[memberId] = GrowthLoadStatus.Loading;
                _growthLoadErrors.Remove(memberId);

                if (_growthLoadCancellations.TryGetValue(memberId, out CancellationTokenSource? previousCts))
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
            {
                Notify();
            }

            return load;
        }

        private async Task<Growth?> HydrateGrowthAsync(Guid memberId, Guid growthId, long generation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AtlasApiDTOsGrowthGrowthDto dto = await _api.AtlasApiEndpointsGrowthGetGrowthEndpointAsync(growthId, cancellationToken);
            if (!IsCurrentGrowthLoad(memberId, generation))
            {
                return null;
            }

            Growth mapped = ApiMappers.MapGrowth(dto);
            TryCommitGrowthLoadResult(memberId, generation, mapped, GrowthLoadStatus.Succeeded);
            return GetGrowth(memberId) ?? mapped;
        }

        private async Task<Growth?> LoadGrowthAsync(Guid memberId, long generation, CancellationToken cancellationToken)
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
                    AtlasApiDTOsGrowthEnsureGrowthForTeamMemberResponse ensured = await _api.AtlasApiEndpointsGrowthEnsureGrowthForTeamMemberEndpointAsync(memberId, cancellationToken);
                    Guid growthId = ensured.GrowthId ?? Guid.Empty;
                    if (growthId == Guid.Empty)
                    {
                        if (!IsCurrentGrowthLoad(memberId, generation))
                        {
                            return null;
                        }

                        LastError = GrowthEnsureFailedMessage;
                        TryCommitGrowthLoadResult(memberId, generation, null, GrowthLoadStatus.Failed, GrowthEnsureFailedMessage);
                        return null;
                    }

                    dto = await _api.AtlasApiEndpointsGrowthGetGrowthEndpointAsync(growthId, cancellationToken);
                }

                if (!IsCurrentGrowthLoad(memberId, generation))
                {
                    return null;
                }

                Growth mapped = ApiMappers.MapGrowth(dto);
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
                {
                    return null;
                }

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

        private async Task<Growth?> EnsureGrowthIdHydrateLoadAsync(Guid memberId, long generation, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                AtlasApiDTOsGrowthEnsureGrowthForTeamMemberResponse ensured = await _api.AtlasApiEndpointsGrowthEnsureGrowthForTeamMemberEndpointAsync(memberId, cancellationToken);
                Guid growthId = ensured.GrowthId ?? Guid.Empty;
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
                {
                    return null;
                }

                Growth? existing = GetGrowth(memberId);
                if (existing is not null && existing.Id != Guid.Empty)
                {
                    return existing;
                }

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
            Growth? cached = GetGrowth(memberId);
            if (cached is not null && cached.Id != Guid.Empty)
            {
                return cached.Id;
            }

            Task<Growth?>? inFlight;
            lock (_gate)
            {
                _growthLoads.TryGetValue(memberId, out inFlight);
            }

            if (inFlight is not null)
            {
                Growth? loaded = await inFlight;
                if (loaded is not null && loaded.Id != Guid.Empty)
                {
                    return loaded.Id;
                }

                cached = GetGrowth(memberId);
                if (cached is not null && cached.Id != Guid.Empty)
                {
                    return cached.Id;
                }
            }
            else if (GetGrowthLoadStatus(memberId) is not GrowthLoadStatus.Failed)
            {
                Growth? loaded = await EnsureGrowthLoadedAsync(memberId, cancellationToken);
                if (loaded is not null && loaded.Id != Guid.Empty)
                {
                    return loaded.Id;
                }

                cached = GetGrowth(memberId);
                if (cached is not null && cached.Id != Guid.Empty)
                {
                    return cached.Id;
                }
            }

            Growth? hydrateLoad = await StartGuardedGrowthLoadAsync(memberId, cancellationToken, EnsureGrowthIdHydrateLoadAsync);
            if (hydrateLoad is not null && hydrateLoad.Id != Guid.Empty)
            {
                return hydrateLoad.Id;
            }

            cached = GetGrowth(memberId);
            return cached?.Id ?? Guid.Empty;
        }

        public void ReplaceTeamMembers(IReadOnlyList<TeamMember> team, IReadOnlyList<TeamMemberRisk> risks)
        {
            Team = team.Select(TeamLogic.WithDerivedActivitySnapshot).ToList();
            TeamMemberRisks = risks.ToList();
            Notify();
        }

        private void SyncProjectLinkedTaskIds(AtlasTask task)
        {
            Projects = Projects.Select(p =>
            {
                var shouldInclude = task.ProjectId == p.Id;
                var has = p.LinkedTaskIds.Contains(task.Id);
                if (shouldInclude && !has)
                {
                    return EntityClone.Project(p, linkedTaskIds: p.LinkedTaskIds.Append(task.Id).ToList());
                }

                if (!shouldInclude && has)
                {
                    return EntityClone.Project(p, linkedTaskIds: p.LinkedTaskIds.Where(id => id != task.Id).ToList());
                }

                return p;
            }).ToList();
        }

        private void SyncRiskLinkedTaskIds(AtlasTask task)
        {
            Risks = Risks.Select(r =>
            {
                var shouldInclude = task.RiskId == r.Id;
                var has = r.LinkedTaskIds.Contains(task.Id);
                if (shouldInclude && !has)
                {
                    return EntityClone.Risk(r, linkedTaskIds: r.LinkedTaskIds.Append(task.Id).ToList());
                }

                if (!shouldInclude && has)
                {
                    return EntityClone.Risk(r, linkedTaskIds: r.LinkedTaskIds.Where(id => id != task.Id).ToList());
                }

                return r;
            }).ToList();
        }

        private void SyncProjectLinkedRiskIds(Risk risk)
        {
            Projects = Projects.Select(p =>
            {
                var shouldInclude = risk.ProjectId == p.Id;
                var has = p.LinkedRiskIds.Contains(risk.Id);
                if (shouldInclude && !has)
                {
                    return EntityClone.Project(p, linkedRiskIds: p.LinkedRiskIds.Append(risk.Id).ToList());
                }

                if (!shouldInclude && has)
                {
                    return EntityClone.Project(p, linkedRiskIds: p.LinkedRiskIds.Where(id => id != risk.Id).ToList());
                }

                return p;
            }).ToList();
        }

        private async Task LoadSettingsAsync(CancellationToken cancellationToken)
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

        private async Task LoadProjectsAsync(CancellationToken cancellationToken)
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

        private async Task LoadProductOwnersAsync(CancellationToken cancellationToken)
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

        private async Task LoadTeamAsync(CancellationToken cancellationToken)
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

        private async Task LoadRisksAsync(CancellationToken cancellationToken)
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

        private async Task LoadTasksAsync(CancellationToken cancellationToken)
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

        private void SetState(ref LoadState field, LoadState value)
        {
            lock (_gate)
            {
                field = value;
            }

            Notify();
        }

        private static void FailIfPending(ref LoadState field)
        {
            if (field == LoadState.Pending)
            {
                field = LoadState.Failed;
            }
        }

        private void Notify() => Changed?.Invoke();

        private enum LoadState
        {
            Pending,
            Ready,
            Failed
        }
    }
}
