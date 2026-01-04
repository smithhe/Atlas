using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;

namespace Atlas.Domain.Entities;

public sealed class TaskItem : AggregateRoot
{
    public string Title { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public TaskStatus Status { get; set; }

    public Guid? AssigneeId { get; set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? RiskId { get; set; }
    public Risk? Risk { get; set; }

    public DateOnly? DueDate { get; set; }

    public string EstimatedDurationText { get; set; } = string.Empty;
    public Confidence EstimateConfidence { get; set; }
    public string? ActualDurationText { get; set; }

    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset LastTouchedAt { get; set; }

    /// <summary>
    /// Tasks this task is blocked by (i.e., dependencies that must complete first).
    /// </summary>
    public List<TaskDependency> BlockedBy { get; set; } = [];
}

