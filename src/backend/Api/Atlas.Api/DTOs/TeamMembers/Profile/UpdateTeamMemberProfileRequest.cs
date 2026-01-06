namespace Atlas.Api.DTOs.TeamMembers.Profile;

public sealed record UpdateTeamMemberProfileRequest(Guid TeamMemberId, string? TimeZone, string? TypicalHours);

