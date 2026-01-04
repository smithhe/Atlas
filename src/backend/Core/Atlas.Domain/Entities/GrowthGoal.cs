using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;

namespace Atlas.Domain.Entities;

public sealed class GrowthGoal : Entity
{
    public string Title { get; set; } = string.Empty;
    public GrowthGoalStatus Status { get; set; }
    
    /// <summary>
    /// Description shown in the growth overview
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public string? Category { get; set; }
    public Priority? Priority { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetDate { get; set; }
    public DateTimeOffset? LastUpdatedAt { get; set; }

    public int? ProgressPercent { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string SuccessCriteria { get; set; } = string.Empty;

    public List<GrowthGoalAction> Actions { get; set; } = [];
    public List<GrowthGoalCheckIn> CheckIns { get; set; } = [];
}

