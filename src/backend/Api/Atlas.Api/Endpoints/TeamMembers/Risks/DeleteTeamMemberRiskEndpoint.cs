using Atlas.Api.DTOs.TeamMembers.Risks;
using Atlas.Application.Features.TeamMembers.Risks.DeleteTeamMemberRisk;

namespace Atlas.Api.Endpoints.TeamMembers.Risks;

public sealed class DeleteTeamMemberRiskEndpoint : Endpoint<DeleteTeamMemberRiskRequest>
{
    private readonly IMediator _mediator;

    public DeleteTeamMemberRiskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/team-members/{teamMemberId:guid}/risks/{teamMemberRiskId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete a team-member-specific risk"; });
    }

    public override async Task HandleAsync(DeleteTeamMemberRiskRequest req, CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        var riskId = Route<Guid>("teamMemberRiskId");
        req = req with { TeamMemberId = teamMemberId, TeamMemberRiskId = riskId };

        var ok = await _mediator.Send(new DeleteTeamMemberRiskCommand(req.TeamMemberId, req.TeamMemberRiskId), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

