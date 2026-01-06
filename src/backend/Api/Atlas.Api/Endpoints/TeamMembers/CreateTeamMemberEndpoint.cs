using Atlas.Api.DTOs.TeamMembers;
using Atlas.Application.Features.TeamMembers.CreateTeamMember;

namespace Atlas.Api.Endpoints.TeamMembers;

public sealed class CreateTeamMemberEndpoint : Endpoint<CreateTeamMemberRequest, CreateTeamMemberResponse>
{
    private readonly IMediator _mediator;

    public CreateTeamMemberEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/team-members");
        AllowAnonymous();
        Summary(s => { s.Summary = "Create a team member"; });
    }

    public override async Task HandleAsync(CreateTeamMemberRequest req, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateTeamMemberCommand(req.Name, req.Role, req.StatusDot), ct);
        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new CreateTeamMemberResponse(id), 201, ct);
    }
}

