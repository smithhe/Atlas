using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.TeamMembers;

public sealed record CreateTeamMemberRequest(string Name, string? Role, StatusDot StatusDot);

