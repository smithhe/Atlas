using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{

/// <summary>Team member mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class TeamMemberService : ITeamMemberService
{
    private readonly IAtlasApiClient _api;
    private readonly IAppCacheService _cache;

    public TeamMemberService(IAtlasApiClient api, IAppCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task UpdateAsync(TeamMember previous, TeamMember next, CancellationToken cancellationToken = default)
    {
        bool memberChanged = previous.Name != next.Name || previous.Role != next.Role || previous.CurrentFocus != next.CurrentFocus;
        bool profileChanged = previous.Profile.TimeZone != next.Profile.TimeZone || previous.Profile.TypicalHours != next.Profile.TypicalHours;
        bool signalsChanged = previous.Signals.Load != next.Signals.Load
            || previous.Signals.Delivery != next.Signals.Delivery
            || previous.Signals.SupportNeeded != next.Signals.SupportNeeded;

        if (!memberChanged && !profileChanged && !signalsChanged)
        {
            return;
        }

        await OptimisticCache.ApplyAsync(
            previous,
            next,
            m => EntityClone.TeamMember(m),
            _cache.UpdateTeamMember,
            async () =>
            {
                List<Task> tasks = [];
                if (memberChanged)
                {
                    tasks.Add(_api.AtlasApiEndpointsTeamMembersUpdateTeamMemberEndpointAsync(
                        next.Id,
                        EntityRequestMappers.ToUpdateTeamMemberRequest(next),
                        cancellationToken));
                }

                if (profileChanged)
                {
                    tasks.Add(_api.AtlasApiEndpointsTeamMembersProfileUpdateTeamMemberProfileEndpointAsync(
                        next.Id,
                        EntityRequestMappers.ToUpdateTeamMemberProfileRequest(next.Profile),
                        cancellationToken));
                }

                if (signalsChanged)
                {
                    tasks.Add(_api.AtlasApiEndpointsTeamMembersSignalsUpdateTeamMemberSignalsEndpointAsync(
                        next.Id,
                        EntityRequestMappers.ToUpdateTeamMemberSignalsRequest(next.Signals),
                        cancellationToken));
                }

                await Task.WhenAll(tasks);
            });
    }
}
}
