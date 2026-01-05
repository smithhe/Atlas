namespace Atlas.Application.Features.TeamMembers.Risks.DeleteTeamMemberRisk;

public sealed record DeleteTeamMemberRiskCommand(Guid TeamMemberId, Guid TeamMemberRiskId) : IRequest<bool>;

