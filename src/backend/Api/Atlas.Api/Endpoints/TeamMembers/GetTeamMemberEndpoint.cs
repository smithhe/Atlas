using Atlas.Api.DTOs.TeamMembers;
using Atlas.Api.Mappers;
using Atlas.Application.Features.TeamMembers.GetTeamMember;

namespace Atlas.Api.Endpoints.TeamMembers;

public sealed class GetTeamMemberEndpoint : Endpoint<GetTeamMemberRequest, TeamMemberDto>
{
    private readonly IMediator _mediator;

    public GetTeamMemberEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/team-members/{id:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get a team member by id"; });
    }

    public override async Task HandleAsync(GetTeamMemberRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        req = req with { Id = id };

        var member = await _mediator.Send(new GetTeamMemberByIdQuery(req.Id, IncludeDetails: true), ct);
        if (member is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(TeamMemberMapper.ToDto(member), ct);
    }
}

