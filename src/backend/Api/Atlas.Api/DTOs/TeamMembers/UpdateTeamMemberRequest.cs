using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.TeamMembers;

public sealed record UpdateTeamMemberRequest(string Name, string? Role, StatusDot StatusDot);

