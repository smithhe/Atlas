using Atlas.Api.DTOs.Growth;
using Atlas.Application.Features.Growth.EnsureGrowthForTeamMember;

namespace Atlas.Api.Endpoints.Growth;

public sealed class EnsureGrowthForTeamMemberEndpoint : Endpoint<EnsureGrowthForTeamMemberRequest, EnsureGrowthForTeamMemberResponse>
{
    private readonly IMediator _mediator;

    public EnsureGrowthForTeamMemberEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/team-members/{teamMemberId:guid}/growth/ensure");
        AllowAnonymous();
        Summary(s => { s.Summary = "Ensure a growth plan exists for a team member"; });
    }

    public override async Task HandleAsync(EnsureGrowthForTeamMemberRequest req, CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        req = req with { TeamMemberId = teamMemberId };

        var id = await _mediator.Send(new EnsureGrowthForTeamMemberCommand(req.TeamMemberId), ct);
        await Send.OkAsync(new EnsureGrowthForTeamMemberResponse(id), ct);
    }
}

