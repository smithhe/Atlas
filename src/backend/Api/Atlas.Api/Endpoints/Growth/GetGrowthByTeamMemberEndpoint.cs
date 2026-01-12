using Atlas.Api.DTOs.Growth;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Growth.GetGrowth;

namespace Atlas.Api.Endpoints.Growth;

public sealed class GetGrowthByTeamMemberEndpoint : EndpointWithoutRequest<GrowthDto>
{
    private readonly IMediator _mediator;

    public GetGrowthByTeamMemberEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/team-members/{teamMemberId:guid}/growth");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get a team member's growth plan"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var teamMemberId = Route<Guid>("teamMemberId");
        var growth = await _mediator.Send(new GetGrowthByTeamMemberIdQuery(teamMemberId), ct);
        if (growth is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(GrowthMapper.ToDto(growth), ct);
    }
}

