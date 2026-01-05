using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.GetTeamMember;

public sealed class GetTeamMemberByIdQueryHandler : IRequestHandler<GetTeamMemberByIdQuery, TeamMember?>
{
    private readonly ITeamMemberRepository _team;

    public GetTeamMemberByIdQueryHandler(ITeamMemberRepository team)
    {
        _team = team;
    }

    public Task<TeamMember?> Handle(GetTeamMemberByIdQuery request, CancellationToken cancellationToken)
    {
        return request.IncludeDetails
            ? _team.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            : _team.GetByIdAsync(request.Id, cancellationToken);
    }
}

