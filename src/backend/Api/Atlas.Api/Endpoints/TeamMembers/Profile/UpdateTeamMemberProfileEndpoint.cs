using Atlas.Api.DTOs.TeamMembers.Profile;
using Atlas.Application.Features.TeamMembers.Profile.UpdateTeamMemberProfile;

namespace Atlas.Api.Endpoints.TeamMembers.Profile;

public sealed class UpdateTeamMemberProfileEndpoint : Endpoint<UpdateTeamMemberProfileRequest>
{
    private readonly IMediator _mediator;

    public UpdateTeamMemberProfileEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/team-members/{teamMemberId:guid}/profile");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update team member profile"; });
    }

    public override async Task HandleAsync(UpdateTeamMemberProfileRequest req, CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        req = req with { TeamMemberId = teamMemberId };

        var ok = await _mediator.Send(new UpdateTeamMemberProfileCommand(req.TeamMemberId, req.TimeZone, req.TypicalHours), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

