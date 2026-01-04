using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;

namespace Atlas.Domain.Entities;

public sealed class TeamMemberRisk : AggregateRoot
{
    public Guid MemberId { get; set; }

    public string Title { get; set; } = string.Empty;

    public TeamMemberRiskSeverity Severity { get; set; }
    public string RiskType { get; set; } = string.Empty;
    public TeamMemberRiskStatus Status { get; set; }
    public TeamMemberRiskTrend Trend { get; set; }

    public DateOnly FirstNoticedDate { get; set; }
    public string ImpactArea { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string CurrentAction { get; set; } = string.Empty;

    public DateTimeOffset? LastReviewedAt { get; set; }

    public Guid? LinkedRiskId { get; set; }
}

