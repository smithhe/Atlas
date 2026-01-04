using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

/// <summary>
/// Join entity modeling a self-referencing dependency between tasks.
/// The dependent task is the owning <see cref="TaskItem"/> (via its <c>BlockedBy</c> collection).
/// This entity stores only the blocker reference.
/// </summary>
public sealed class TaskDependency : Entity
{
    public Guid TaskId { get; set; }
    public TaskItem? Task { get; set; }
}

