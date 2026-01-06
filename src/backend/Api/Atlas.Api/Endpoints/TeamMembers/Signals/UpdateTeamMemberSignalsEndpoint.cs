using Atlas.Api.DTOs.TeamMembers.Signals;
using Atlas.Application.Features.TeamMembers.Signals.UpdateTeamMemberSignals;

namespace Atlas.Api.Endpoints.TeamMembers.Signals;

public sealed class UpdateTeamMemberSignalsEndpoint : Endpoint<UpdateTeamMemberSignalsRequest>
{
    private readonly IMediator _mediator;

    public UpdateTeamMemberSignalsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/team-members/{teamMemberId:guid}/signals");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update team member signals"; });
    }

    public override async Task HandleAsync(UpdateTeamMemberSignalsRequest req, CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        req = req with { TeamMemberId = teamMemberId };

        var ok = await _mediator.Send(new UpdateTeamMemberSignalsCommand(req.TeamMemberId, req.Load, req.Delivery, req.SupportNeeded), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

