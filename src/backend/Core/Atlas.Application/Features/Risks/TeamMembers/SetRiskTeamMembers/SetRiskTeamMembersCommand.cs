namespace Atlas.Application.Features.Risks.TeamMembers.SetRiskTeamMembers;

public sealed record SetRiskTeamMembersCommand(Guid RiskId, IReadOnlyList<Guid> TeamMemberIds) : IRequest<bool>;
