using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Per-member risk mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class TeamMemberRiskService
{
    private readonly IAtlasApiClient _api;
    private readonly AppCacheService _cache;

    public TeamMemberRiskService(IAtlasApiClient api, AppCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<TeamMemberRisk> AddAsync(
        Guid memberId,
        TeamMemberRisk draft,
        CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsTeamMembersRisksAddTeamMemberRiskResponse res =
            await _api.AtlasApiEndpointsTeamMembersRisksAddTeamMemberRiskEndpointAsync(
                memberId,
                EntityRequestMappers.ToAddTeamMemberRiskRequest(draft),
                cancellationToken);

        draft.Id = res.Id ?? Guid.NewGuid();
        draft.MemberId = memberId;
        _cache.AddTeamMemberRisk(draft);
        return draft;
    }

    public async Task UpdateAsync(Guid memberId, TeamMemberRisk next, CancellationToken cancellationToken = default)
    {
        TeamMemberRisk? previous = _cache.TeamMemberRisks.FirstOrDefault(r => r.Id == next.Id && r.MemberId == memberId);
        TeamMemberRisk? rollback = previous is not null ? EntityClone.TeamMemberRisk(previous) : null;
        _cache.UpdateTeamMemberRisk(next);
        try
        {
            await _api.AtlasApiEndpointsTeamMembersRisksUpdateTeamMemberRiskEndpointAsync(
                memberId,
                next.Id,
                EntityRequestMappers.ToUpdateTeamMemberRiskRequest(next),
                cancellationToken);
        }
        catch
        {
            if (rollback is not null)
            {
                _cache.UpdateTeamMemberRisk(rollback);
            }

            throw;
        }
    }
}
