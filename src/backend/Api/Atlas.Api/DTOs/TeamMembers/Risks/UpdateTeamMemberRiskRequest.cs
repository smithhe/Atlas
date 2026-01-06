using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.TeamMembers.Risks;

public sealed record UpdateTeamMemberRiskRequest(
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
    Guid? LinkedGlobalRiskId);

