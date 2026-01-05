using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.ListTeamMembers;

public sealed class ListTeamMembersQueryHandler : IRequestHandler<ListTeamMembersQuery, IReadOnlyList<TeamMember>>
{
    private readonly ITeamMemberRepository _team;

    public ListTeamMembersQueryHandler(ITeamMemberRepository team)
    {
        _team = team;
    }

    public Task<IReadOnlyList<TeamMember>> Handle(ListTeamMembersQuery request, CancellationToken cancellationToken)
    {
        return _team.ListAsync(cancellationToken);
    }
}

