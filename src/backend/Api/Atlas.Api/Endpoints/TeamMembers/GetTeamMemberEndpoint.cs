using Atlas.Api.DTOs.TeamMembers;
using Atlas.Api.Mappers;
using Atlas.Application.Features.TeamMembers.GetTeamMember;
using Atlas.Domain.Entities;

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
        Guid id = Route<Guid>("id");
        req = new GetTeamMemberRequest(Id: id);

        TeamMember? member = await _mediator.Send(new GetTeamMemberByIdQuery(req.Id, IncludeDetails: true), ct);
        if (member is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(TeamMemberMapper.ToDto(member), ct);
    }
}

