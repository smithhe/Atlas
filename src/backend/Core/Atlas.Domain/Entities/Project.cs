using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;
using Atlas.Domain.ValueObjects;

namespace Atlas.Domain.Entities;

public sealed class Project : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ProjectStatus? Status { get; set; }
    public HealthSignal? Health { get; set; }

    public DateOnly? TargetDate { get; set; }
    public Priority? Priority { get; set; }

    public Guid? ProductOwnerId { get; set; }

    public List<string> Tags { get; set; } = [];
    public List<ProjectLink> Links { get; set; } = [];

    public DateTimeOffset? LastUpdatedAt { get; set; }
    public ProjectCheckIn? LatestCheckIn { get; set; }

    public List<Guid> LinkedTaskIds { get; set; } = [];
    public List<Guid> LinkedRiskIds { get; set; } = [];
    public List<Guid> TeamMemberIds { get; set; } = [];
}

