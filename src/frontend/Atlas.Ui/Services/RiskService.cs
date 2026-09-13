using System.Collections.Concurrent;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Risk mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class RiskService
{
    private readonly IAtlasApiClient _api;
    private readonly AppCacheService _cache;
    private readonly TimeSpan? _debounceDelay;
    private readonly ConcurrentDictionary<Guid, EntityAutosaveCoordinator<Risk>> _coordinators = new();

    public event Action<Guid>? SaveStateChanged;

    public RiskService(IAtlasApiClient api, AppCacheService cache)
        : this(api, cache, debounceDelay: null)
    {
    }

    internal RiskService(IAtlasApiClient api, AppCacheService cache, TimeSpan? debounceDelay)
    {
        _api = api;
        _cache = cache;
        _debounceDelay = debounceDelay;
    }

    public EntitySaveState GetSaveState(Guid riskId) =>
        _coordinators.TryGetValue(riskId, out EntityAutosaveCoordinator<Risk>? coordinator)
            ? coordinator.State
            : EntitySaveState.Idle;

    public async Task<Risk> CreateAsync(Risk draft, CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsRisksCreateRiskResponse res =
            await _api.AtlasApiEndpointsRisksCreateRiskEndpointAsync(
                EntityRequestMappers.ToCreateRiskRequest(draft),
                cancellationToken);
        draft.Id = res.Id ?? Guid.Empty;
        _cache.AddRisk(draft);
        return draft;
    }

    public Task UpdateAsync(Guid riskId, Func<Risk, Risk> edit, bool debounce = false, CancellationToken cancellationToken = default)
    {
        _ = _cache.TryGetRisk(riskId)
            ?? throw new InvalidOperationException($"Risk {riskId} is not in the cache.");
        EntityAutosaveCoordinator<Risk> coordinator = GetCoordinator(riskId);
        return coordinator.SaveAsync(
            edit,
            () => _cache.TryGetRisk(riskId),
            _cache.UpdateRisk,
            ct => PersistAsync(riskId, ct),
            debounce,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid riskId, CancellationToken cancellationToken = default)
    {
        await _api.AtlasApiEndpointsRisksDeleteRiskEndpointAsync(riskId, cancellationToken);
        _cache.RemoveRisk(riskId);
        _coordinators.TryRemove(riskId, out _);
    }

    public Task SetTeamMembersAsync(Guid riskId, IReadOnlyList<Guid> memberIds, CancellationToken cancellationToken = default)
    {
        _ = _cache.TryGetRisk(riskId)
            ?? throw new InvalidOperationException($"Risk {riskId} is not in the cache.");
        EntityAutosaveCoordinator<Risk> coordinator = GetCoordinator(riskId);
        return coordinator.SaveAsync(
            risk => EntityClone.Risk(
                risk,
                linkedTeamMemberIds: memberIds.ToList(),
                lastUpdatedIso: DateTimeOffset.UtcNow.ToString("o")),
            () => _cache.TryGetRisk(riskId),
            _cache.UpdateRisk,
            ct => PersistTeamMembersAsync(riskId, ct),
            debounce: false,
            refetchAsync: ct => RefetchRiskAsync(riskId, ct),
            cancellationToken);
    }

    private EntityAutosaveCoordinator<Risk> GetCoordinator(Guid riskId) =>
        _coordinators.GetOrAdd(riskId, _ =>
        {
            EntityAutosaveCoordinator<Risk> coordinator = _debounceDelay is null
                ? new EntityAutosaveCoordinator<Risk>()
                : new EntityAutosaveCoordinator<Risk>(_debounceDelay);
            coordinator.StateChanged += () => SaveStateChanged?.Invoke(riskId);
            return coordinator;
        });

    private Task PersistAsync(Guid riskId, CancellationToken cancellationToken)
    {
        Risk risk = _cache.TryGetRisk(riskId)
            ?? throw new InvalidOperationException($"Risk {riskId} is not in the cache.");
        return _api.AtlasApiEndpointsRisksUpdateRiskEndpointAsync(
            risk.Id,
            EntityRequestMappers.ToUpdateRiskRequest(risk),
            cancellationToken);
    }

    private async Task PersistTeamMembersAsync(Guid riskId, CancellationToken cancellationToken)
    {
        Risk risk = _cache.TryGetRisk(riskId)
            ?? throw new InvalidOperationException($"Risk {riskId} is not in the cache.");
        await _api.AtlasApiEndpointsRisksSetRiskTeamMembersEndpointAsync(
            riskId,
            EntityRequestMappers.ToSetRiskTeamMembersRequest(risk),
            cancellationToken);
        // persistLatestAsync must write the whole cached entity so cancelled debounced text edits persist too.
        await PersistAsync(riskId, cancellationToken);
    }

    private async Task RefetchRiskAsync(Guid riskId, CancellationToken cancellationToken)
    {
        AtlasApiDTOsRisksRiskDto dto = await _api.AtlasApiEndpointsRisksGetRiskEndpointAsync(riskId, cancellationToken);
        RiskLookups lookups = new()
        {
            ProjectNameById = _cache.Projects.ToDictionary(p => p.Id, p => p.Name)
        };
        Risk risk = ApiMappers.MapRisk(dto, lookups);
        _cache.UpdateRisk(risk);
    }
}
