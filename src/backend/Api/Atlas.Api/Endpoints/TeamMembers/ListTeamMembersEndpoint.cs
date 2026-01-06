using Atlas.Api.DTOs.TeamMembers;
using Atlas.Api.Mappers;
using Atlas.Application.Features.TeamMembers.ListTeamMembers;

namespace Atlas.Api.Endpoints.TeamMembers;

public sealed class ListTeamMembersEndpoint : Endpoint<ListTeamMembersRequest, IReadOnlyList<TeamMemberListItemDto>>
{
    private readonly IMediator _mediator;

    public ListTeamMembersEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/team-members");
        AllowAnonymous();
        Summary(s => { s.Summary = "List team members"; });
    }

    public override async Task HandleAsync(ListTeamMembersRequest req, CancellationToken ct)
    {
        var members = await _mediator.Send(new ListTeamMembersQuery(), ct);

        if (req.Ids is { Count: > 0 })
        {
            var set = new HashSet<Guid>(req.Ids.Where(x => x != Guid.Empty));
            members = members.Where(m => set.Contains(m.Id)).ToList();
        }

        var dtos = members.Select(TeamMemberMapper.ToListItemDto).ToList();
        await Send.OkAsync(dtos, ct);
    }
}

