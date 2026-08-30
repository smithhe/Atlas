using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
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
            await _api.AtlasApiEndpointsRisksCreateRiskEndpointAsync(
                EntityRequestMappers.ToCreateRiskRequest(draft, _cache.Projects),
                cancellationToken);
        draft.Id = res.Id ?? Guid.Empty;
        _cache.AddRisk(draft);
        return draft;
    }

    public async Task UpdateAsync(Risk risk, CancellationToken cancellationToken = default)
    {
        Risk? previous = _cache.Risks.FirstOrDefault(r => r.Id == risk.Id);
        Risk? previousClone = previous is not null ? EntityClone.Risk(previous) : null;
        _cache.UpdateRisk(risk);
        try
        {
            await _api.AtlasApiEndpointsRisksUpdateRiskEndpointAsync(
                risk.Id,
                EntityRequestMappers.ToUpdateRiskRequest(risk, _cache.Projects),
                cancellationToken);
        }
        catch
        {
            if (previousClone is not null)
            {
                _cache.UpdateRisk(previousClone);
            }

            throw;
        }
    }

    public async Task DeleteAsync(Guid riskId, CancellationToken cancellationToken = default)
    {
        await _api.AtlasApiEndpointsRisksDeleteRiskEndpointAsync(riskId, cancellationToken);
        _cache.RemoveRisk(riskId);
    }

    public async Task SetTeamMembersAsync(Risk risk, CancellationToken cancellationToken = default)
    {
        Risk? previous = _cache.Risks.FirstOrDefault(r => r.Id == risk.Id);
        Risk? previousClone = previous is not null ? EntityClone.Risk(previous) : null;
        _cache.UpdateRisk(risk);
        try
        {
            await _api.AtlasApiEndpointsRisksSetRiskTeamMembersEndpointAsync(
                risk.Id,
                new AtlasApiDTOsRisksSetRiskTeamMembersRequest { TeamMemberIds = risk.LinkedTeamMemberIds.ToList() },
                cancellationToken);
        }
        catch
        {
            if (previousClone is not null)
            {
                _cache.UpdateRisk(previousClone);
            }

            throw;
        }
    }
}
