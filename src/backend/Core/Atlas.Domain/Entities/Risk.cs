using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;
using Atlas.Domain.ValueObjects;

namespace Atlas.Domain.Entities;

public sealed class Risk : AggregateRoot
{
    public string Title { get; set; } = string.Empty;
    public RiskStatus Status { get; set; }
    public SeverityLevel Severity { get; set; }

    public Guid? ProjectId { get; set; }
    public string? Mitigation { get; set; }

    public string Description { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;

    public List<Guid> LinkedTaskIds { get; set; } = [];
    public List<Guid> LinkedTeamMemberIds { get; set; } = [];

    public List<RiskHistoryEntry> History { get; set; } = [];
    public DateTimeOffset LastUpdatedAt { get; set; }
}

