namespace Atlas.Api.DTOs.TeamMembers;

/// <summary>
/// Placeholder for future filtering/pagination (also supports fetching a specific set by ids).
/// </summary>
public sealed record ListTeamMembersRequest(IReadOnlyList<Guid>? Ids = null);

