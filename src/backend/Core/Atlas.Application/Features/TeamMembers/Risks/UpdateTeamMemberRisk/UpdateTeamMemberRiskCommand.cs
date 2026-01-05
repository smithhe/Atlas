using Atlas.Domain.Enums;

namespace Atlas.Application.Features.TeamMembers.Risks.UpdateTeamMemberRisk;

public sealed record UpdateTeamMemberRiskCommand(
    Guid TeamMemberId,
    Guid TeamMemberRiskId,
    string Title,
    TeamMemberRiskSeverity Severity,
    string RiskType,
    TeamMemberRiskStatus Status,
    TeamMemberRiskTrend Trend,
    DateOnly FirstNoticedDate,
    string ImpactArea,
    string Description,
    string CurrentAction,
    Guid? LinkedGlobalRiskId) : IRequest<bool>;

