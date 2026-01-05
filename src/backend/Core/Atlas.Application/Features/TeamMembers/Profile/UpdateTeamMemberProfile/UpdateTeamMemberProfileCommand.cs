namespace Atlas.Application.Features.TeamMembers.Profile.UpdateTeamMemberProfile;

public sealed record UpdateTeamMemberProfileCommand(
    Guid TeamMemberId,
    string? TimeZone,
    string? TypicalHours) : IRequest<bool>;

