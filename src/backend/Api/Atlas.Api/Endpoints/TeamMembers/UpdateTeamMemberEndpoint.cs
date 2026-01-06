using Atlas.Api.DTOs.TeamMembers;
using Atlas.Application.Features.TeamMembers.UpdateTeamMember;

namespace Atlas.Api.Endpoints.TeamMembers;

public sealed class UpdateTeamMemberEndpoint : Endpoint<UpdateTeamMemberRequest>
{
    private readonly IMediator _mediator;

    public UpdateTeamMemberEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/team-members/{id:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update a team member"; });
    }

    public override async Task HandleAsync(UpdateTeamMemberRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var ok = await _mediator.Send(new UpdateTeamMemberCommand(id, req.Name, req.Role, req.StatusDot), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

