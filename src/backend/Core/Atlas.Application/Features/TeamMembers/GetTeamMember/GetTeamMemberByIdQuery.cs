using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.GetTeamMember;

public sealed record GetTeamMemberByIdQuery(Guid Id, bool IncludeDetails = true) : IRequest<TeamMember?>;

