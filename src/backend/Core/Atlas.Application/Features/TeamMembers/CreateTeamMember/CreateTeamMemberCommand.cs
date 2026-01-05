using Atlas.Domain.Enums;

namespace Atlas.Application.Features.TeamMembers.CreateTeamMember;

public sealed record CreateTeamMemberCommand(
    string Name,
    string? Role,
    StatusDot StatusDot) : IRequest<Guid>;

