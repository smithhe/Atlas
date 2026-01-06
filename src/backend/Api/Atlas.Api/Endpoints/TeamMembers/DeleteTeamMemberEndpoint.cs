using Atlas.Api.DTOs.TeamMembers;
using Atlas.Application.Features.TeamMembers.DeleteTeamMember;

namespace Atlas.Api.Endpoints.TeamMembers;

public sealed class DeleteTeamMemberEndpoint : Endpoint<DeleteTeamMemberRequest>
{
    private readonly IMediator _mediator;

    public DeleteTeamMemberEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/team-members/{id:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete a team member"; });
    }

    public override async Task HandleAsync(DeleteTeamMemberRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        req = req with { Id = id };

        var ok = await _mediator.Send(new DeleteTeamMemberCommand(req.Id), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

