using System.Collections.Generic;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>
/// App-wide cache / hydration skeleton mirroring React TanStack query topology.
/// Parallel roots: settings, projects, productOwners, team.
/// Chain: projects → risks → tasks.
/// <see cref="IsHydrating"/> is true while any of the six are still pending
/// (including waiting on prerequisites — same as TanStack <c>isPending</c> while <c>enabled: false</c>).
/// </summary>
public sealed class AppCacheService
{
    readonly IAtlasApiClient _api;
    readonly object _gate = new();

    Task? _hydration;

    LoadState _settings = LoadState.Pending;
    LoadState _projects = LoadState.Pending;
    LoadState _productOwners = LoadState.Pending;
    LoadState _team = LoadState.Pending;
    LoadState _risks = LoadState.Pending;
    LoadState _tasks = LoadState.Pending;

    public AppCacheService(IAtlasApiClient api)
    {
        _api = api;
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

    public Settings? Settings { get; private set; }
    public IReadOnlyList<Project> Projects { get; private set; } = Array.Empty<Project>();
    public IReadOnlyList<ProductOwner> ProductOwners { get; private set; } = Array.Empty<ProductOwner>();
    public IReadOnlyList<TeamMember> Team { get; private set; } = Array.Empty<TeamMember>();
    public IReadOnlyList<TeamMemberRisk> TeamMemberRisks { get; private set; } = Array.Empty<TeamMemberRisk>();
    public IReadOnlyList<Risk> Risks { get; private set; } = Array.Empty<Risk>();
    public IReadOnlyList<AtlasTask> Tasks { get; private set; } = Array.Empty<AtlasTask>();

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
            // Four independent roots start together.
            Task settingsTask = LoadSettingsAsync(cancellationToken);
            Task projectsTask = LoadProjectsAsync(cancellationToken);
            Task productOwnersTask = LoadProductOwnersAsync(cancellationToken);
            Task teamTask = LoadTeamAsync(cancellationToken);

            await Task.WhenAll(settingsTask, projectsTask, productOwnersTask, teamTask);

            // risks enabled only after projects succeed
            await LoadRisksAsync(cancellationToken);

            // tasks enabled only after projects AND risks succeed
            await LoadTasksAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            // Mark anything still pending as failed so IsHydrating becomes false.
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

    async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            AtlasApiDTOsSettingsSettingsDto dto = await _api.AtlasApiEndpointsSettingsGetSettingsEndpointAsync(cancellationToken);
            Settings = ApiMappers.MapSettings(dto);
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
            // Dependents cannot load — mark Failed so IsHydrating clears.
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
            // Prerequisite failed/missing — mark Failed so IsHydrating clears.
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
            // Tasks cannot load without risks — mark Failed so IsHydrating clears.
            SetState(ref _tasks, LoadState.Failed);
        }
    }

    async Task LoadTasksAsync(CancellationToken cancellationToken)
    {
        if (_projects != LoadState.Ready || _risks != LoadState.Ready)
        {
            // Prerequisites failed/missing — mark Failed so IsHydrating clears.
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
