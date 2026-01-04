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
    public ProductOwner? ProductOwner { get; set; }

    public List<ProjectTag> Tags { get; set; } = [];
    public List<ProjectLinkItem> Links { get; set; } = [];

    public DateTimeOffset? LastUpdatedAt { get; set; }
    public ProjectCheckIn? LatestCheckIn { get; set; }

    /// <summary>
    /// Tasks associated with this project.
    /// </summary>
    public List<TaskItem> Tasks { get; set; } = [];

    /// <summary>
    /// Risks associated with this project.
    /// </summary>
    public List<Risk> Risks { get; set; } = [];

    /// <summary>
    /// Team membership for this project (explicit many-to-many join).
    /// </summary>
    public List<ProjectTeamMember> TeamMembers { get; set; } = [];
}

