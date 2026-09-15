using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{

/// <summary>Per-member risk mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class TeamMemberRiskService : ITeamMemberRiskService
{
    private readonly IAtlasApiClient _api;
    private readonly IAppCacheService _cache;

    public TeamMemberRiskService(IAtlasApiClient api, IAppCacheService cache)
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

        if (res.Id is not Guid riskId || riskId == Guid.Empty)
        {
            throw new InvalidOperationException("Add team member risk response did not include a risk id.");
        }

        draft.Id = riskId;
        draft.MemberId = memberId;
        _cache.AddTeamMemberRisk(draft);
        return draft;
    }

    public Task UpdateAsync(Guid memberId, TeamMemberRisk next, CancellationToken cancellationToken = default)
    {
        TeamMemberRisk? previous = _cache.TeamMemberRisks.FirstOrDefault(r => r.Id == next.Id && r.MemberId == memberId);
        return OptimisticCache.ApplyAsync(
            previous,
            next,
            r => EntityClone.TeamMemberRisk(r),
            _cache.UpdateTeamMemberRisk,
            () => _api.AtlasApiEndpointsTeamMembersRisksUpdateTeamMemberRiskEndpointAsync(
                memberId,
                next.Id,
                EntityRequestMappers.ToUpdateTeamMemberRiskRequest(next),
                cancellationToken));
    }
}
}
