using Atlas.Api.DTOs.Projects;
using Atlas.Application.Features.Projects.TeamMembers.SetProjectTeamMembers;

namespace Atlas.Api.Endpoints.Projects;

public sealed class SetProjectTeamMembersEndpoint : Endpoint<SetProjectTeamMembersRequest>
{
    private readonly IMediator _mediator;

    public SetProjectTeamMembersEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/projects/{id:guid}/team-members");
        AllowAnonymous();
        Summary(s => { s.Summary = "Set the team-member membership for a project"; });
    }

    public override async Task HandleAsync(SetProjectTeamMembersRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var ok = await _mediator.Send(new SetProjectTeamMembersCommand(id, req.TeamMemberIds), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

