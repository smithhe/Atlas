using Atlas.Api.DTOs.TeamMembers.Risks;
using Atlas.Application.Features.TeamMembers.Risks.UpdateTeamMemberRisk;

namespace Atlas.Api.Endpoints.TeamMembers.Risks;

public sealed class UpdateTeamMemberRiskEndpoint : Endpoint<UpdateTeamMemberRiskRequest>
{
    private readonly IMediator _mediator;

    public UpdateTeamMemberRiskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/team-members/{teamMemberId:guid}/risks/{teamMemberRiskId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update a team-member-specific risk"; });
    }

    public override async Task HandleAsync(UpdateTeamMemberRiskRequest req, CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        var riskId = Route<Guid>("teamMemberRiskId");
        req = req with { TeamMemberId = teamMemberId, TeamMemberRiskId = riskId };

        var ok = await _mediator.Send(new UpdateTeamMemberRiskCommand(
            req.TeamMemberId,
            req.TeamMemberRiskId,
            req.Title,
            req.Severity,
            req.RiskType,
            req.Status,
            req.Trend,
            req.FirstNoticedDate,
            req.ImpactArea,
            req.Description,
            req.CurrentAction,
            req.LinkedGlobalRiskId), ct);

        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

