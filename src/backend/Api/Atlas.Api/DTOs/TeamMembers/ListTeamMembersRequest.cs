namespace Atlas.Api.DTOs.TeamMembers;

/// <summary>
/// Placeholder for future filtering/pagination (also supports fetching a specific set by ids).
/// </summary>
public sealed class ListTeamMembersRequest
{
    [QueryParam]
    public IReadOnlyList<Guid>? Ids { get; set; }
}

