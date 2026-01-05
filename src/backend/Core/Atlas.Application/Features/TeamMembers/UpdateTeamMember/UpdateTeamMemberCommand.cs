using Atlas.Domain.Enums;

namespace Atlas.Application.Features.TeamMembers.UpdateTeamMember;

public sealed record UpdateTeamMemberCommand(
    Guid Id,
    string Name,
    string? Role,
    StatusDot StatusDot) : IRequest<bool>;

