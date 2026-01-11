namespace Atlas.Api.DTOs.Projects;

public sealed record SetProjectTeamMembersRequest(IReadOnlyList<Guid> TeamMemberIds);

