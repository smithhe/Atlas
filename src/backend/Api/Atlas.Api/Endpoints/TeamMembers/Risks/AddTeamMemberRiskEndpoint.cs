using Atlas.Api.DTOs.TeamMembers.Risks;
using Atlas.Application.Features.TeamMembers.Risks.AddTeamMemberRisk;

namespace Atlas.Api.Endpoints.TeamMembers.Risks;

public sealed class AddTeamMemberRiskEndpoint : Endpoint<AddTeamMemberRiskRequest, AddTeamMemberRiskResponse>
{
    private readonly IMediator _mediator;

    public AddTeamMemberRiskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/team-members/{teamMemberId:guid}/risks");
        AllowAnonymous();
        Summary(s => { s.Summary = "Add a team-member-specific risk"; });
    }

    public override async Task HandleAsync(AddTeamMemberRiskRequest req, CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        req = req with { TeamMemberId = teamMemberId };

        var id = await _mediator.Send(new AddTeamMemberRiskCommand(
            req.TeamMemberId,
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

        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new AddTeamMemberRiskResponse(id), 201, ct);
    }
}

