using Atlas.Ui.Api.Generated;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Risk mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class RiskService
{
    private readonly IAtlasApiClient _api;
    private readonly AppCacheService _cache;

    public RiskService(IAtlasApiClient api, AppCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<Risk> CreateAsync(Risk draft, CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsRisksCreateRiskResponse res =
            await _api.AtlasApiEndpointsRisksCreateRiskEndpointAsync(_cache.ToCreateRiskRequest(draft), cancellationToken);
        draft.Id = res.Id ?? Guid.Empty;
        _cache.AddRisk(draft);
        return draft;
    }

    public Task UpdateAsync(Risk risk, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsRisksUpdateRiskEndpointAsync(risk.Id, _cache.ToUpdateRiskRequest(risk), cancellationToken);

    public async Task DeleteAsync(Guid riskId, CancellationToken cancellationToken = default)
    {
        await _api.AtlasApiEndpointsRisksDeleteRiskEndpointAsync(riskId, cancellationToken);
        _cache.RemoveRisk(riskId);
    }

    public Task SetTeamMembersAsync(Guid riskId, IReadOnlyList<Guid> memberIds, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsRisksSetRiskTeamMembersEndpointAsync(
            riskId,
            new AtlasApiDTOsRisksSetRiskTeamMembersRequest { TeamMemberIds = memberIds.ToList() },
            cancellationToken);
}
