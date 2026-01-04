using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

/// <summary>
/// Join entity modeling a self-referencing dependency between tasks.
/// A dependency indicates: DependentTask is blocked by BlockerTask.
/// </summary>
public sealed class TaskDependency : Entity
{
    public Guid DependentTaskId { get; set; }
    public TaskItem? DependentTask { get; set; }

    public Guid BlockerTaskId { get; set; }
    public TaskItem? BlockerTask { get; set; }
}

