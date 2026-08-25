using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Team member mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class TeamMemberService
{
    private readonly IAtlasApiClient _api;

    public TeamMemberService(IAtlasApiClient api)
    {
        _api = api;
    }

    public async Task UpdateAsync(TeamMember previous, TeamMember next, CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];
        if (previous.Name != next.Name || previous.Role != next.Role || previous.CurrentFocus != next.CurrentFocus)
        {
            tasks.Add(_api.AtlasApiEndpointsTeamMembersUpdateTeamMemberEndpointAsync(next.Id, new AtlasApiDTOsTeamMembersUpdateTeamMemberRequest
            {
                Name = next.Name,
                Role = next.Role,
                StatusDot = ApiMappers.ToApiStatusDot(next.StatusDot),
                CurrentFocus = next.CurrentFocus
            }, cancellationToken));
        }

        if (previous.Profile.TimeZone != next.Profile.TimeZone || previous.Profile.TypicalHours != next.Profile.TypicalHours)
        {
            tasks.Add(_api.AtlasApiEndpointsTeamMembersProfileUpdateTeamMemberProfileEndpointAsync(next.Id, new AtlasApiDTOsTeamMembersProfileUpdateTeamMemberProfileRequest
            {
                TimeZone = next.Profile.TimeZone,
                TypicalHours = next.Profile.TypicalHours
            }, cancellationToken));
        }

        if (previous.Signals.Load != next.Signals.Load
            || previous.Signals.Delivery != next.Signals.Delivery
            || previous.Signals.SupportNeeded != next.Signals.SupportNeeded)
        {
            tasks.Add(_api.AtlasApiEndpointsTeamMembersSignalsUpdateTeamMemberSignalsEndpointAsync(next.Id, new AtlasApiDTOsTeamMembersSignalsUpdateTeamMemberSignalsRequest
            {
                Load = ApiMappers.ToApiLoad(next.Signals.Load),
                Delivery = ApiMappers.ToApiDelivery(next.Signals.Delivery),
                SupportNeeded = ApiMappers.ToApiSupport(next.Signals.SupportNeeded)
            }, cancellationToken));
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }
}
