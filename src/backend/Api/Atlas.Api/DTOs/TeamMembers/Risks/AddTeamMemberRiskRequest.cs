using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.TeamMembers.Risks;

public sealed record AddTeamMemberRiskRequest(
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
    Guid? LinkedGlobalRiskId);

