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
            await _api.AtlasApiEndpointsTeamMembersRisksAddTeamMemberRiskEndpointAsync(memberId, new AtlasApiDTOsTeamMembersRisksAddTeamMemberRiskRequest
            {
                Title = draft.Title,
                Severity = ApiMappers.ToApiTeamMemberRiskSeverity(draft.Severity),
                RiskType = draft.RiskType,
                Status = ApiMappers.ToApiTeamMemberRiskStatus(draft.Status),
                Trend = ApiMappers.ToApiTeamMemberRiskTrend(draft.Trend),
                FirstNoticedDate = DateTimeOffset.TryParse(draft.FirstNoticedDateIso, out DateTimeOffset d) ? d : null,
                ImpactArea = draft.ImpactArea,
                Description = draft.Description,
                CurrentAction = draft.CurrentAction,
                LinkedGlobalRiskId = draft.LinkedRiskId
            }, cancellationToken);

        draft.Id = res.Id ?? Guid.NewGuid();
        draft.MemberId = memberId;
        _cache.AddTeamMemberRisk(draft);
        return draft;
    }

    public Task UpdateAsync(Guid memberId, TeamMemberRisk next, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsTeamMembersRisksUpdateTeamMemberRiskEndpointAsync(memberId, next.Id, new AtlasApiDTOsTeamMembersRisksUpdateTeamMemberRiskRequest
        {
            Title = next.Title,
            Severity = ApiMappers.ToApiTeamMemberRiskSeverity(next.Severity),
            RiskType = next.RiskType,
            Status = ApiMappers.ToApiTeamMemberRiskStatus(next.Status),
            Trend = ApiMappers.ToApiTeamMemberRiskTrend(next.Trend),
            FirstNoticedDate = DateTimeOffset.TryParse(next.FirstNoticedDateIso, out DateTimeOffset d) ? d : null,
            ImpactArea = next.ImpactArea,
            Description = next.Description,
            CurrentAction = next.CurrentAction,
            LinkedGlobalRiskId = next.LinkedRiskId,
            LastReviewedAt = DateTimeOffset.TryParse(next.LastReviewedIso, out DateTimeOffset lr) ? lr : null
        }, cancellationToken);
}
