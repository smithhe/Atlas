using Atlas.Api.DTOs.Risks;
using Atlas.Application.Features.Risks.TeamMembers.SetRiskTeamMembers;

namespace Atlas.Api.Endpoints.Risks;

public sealed class SetRiskTeamMembersEndpoint : Endpoint<SetRiskTeamMembersRequest>
{
    private readonly IMediator _mediator;

    public SetRiskTeamMembersEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/risks/{id:guid}/team-members");
        AllowAnonymous();
        Summary(s => { s.Summary = "Set the team-member links for a risk"; });
    }

    public override async Task HandleAsync(SetRiskTeamMembersRequest req, CancellationToken ct)
    {
        Guid id = Route<Guid>("id");

        try
        {
            var ok = await _mediator.Send(new SetRiskTeamMembersCommand(id, req.TeamMemberIds), ct);
            if (!ok)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
