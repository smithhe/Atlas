using Atlas.Domain.Enums;

namespace Atlas.Application.Features.TeamMembers.Risks.AddTeamMemberRisk;

public sealed record AddTeamMemberRiskCommand(
    Guid TeamMemberId,
    string Title,
    TeamMemberRiskSeverity Severity,
    string RiskType,
    TeamMemberRiskStatus Status,
    TeamMemberRiskTrend Trend,
    DateOnly FirstNoticedDate,
    string ImpactArea,
    string Description,
    string CurrentAction,
    Guid? LinkedGlobalRiskId) : IRequest<Guid>;

