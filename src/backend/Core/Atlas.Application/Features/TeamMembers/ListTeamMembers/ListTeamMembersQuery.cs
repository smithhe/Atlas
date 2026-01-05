using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.ListTeamMembers;

public sealed record ListTeamMembersQuery : IRequest<IReadOnlyList<TeamMember>>;

