namespace Atlas.Api.DTOs.Risks;

public sealed record SetRiskTeamMembersRequest(IReadOnlyList<Guid> TeamMemberIds);
