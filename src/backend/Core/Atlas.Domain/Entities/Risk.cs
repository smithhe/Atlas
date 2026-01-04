using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;

namespace Atlas.Domain.Entities;

public sealed class Risk : AggregateRoot
{
    public string Title { get; set; } = string.Empty;
    public RiskStatus Status { get; set; }
    public SeverityLevel Severity { get; set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Description { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;

    /// <summary>
    /// Tasks linked to this risk.
    /// </summary>
    public List<TaskItem> Tasks { get; set; } = [];

    /// <summary>
    /// Team members linked to this risk (explicit many-to-many join).
    /// </summary>
    public List<RiskTeamMember> LinkedTeamMembers { get; set; } = [];

    public List<RiskHistoryEntry> History { get; set; } = [];
    public DateTimeOffset LastUpdatedAt { get; set; }
}

