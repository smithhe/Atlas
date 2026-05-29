using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class AzureWorkItemLocalNote : Entity
{
    public Guid TeamMemberId { get; set; }
    public TeamMember? TeamMember { get; set; }

    /// <summary>
    /// Azure DevOps work item number (not the internal <see cref="AzureWorkItem"/> id).
    /// </summary>
    public int WorkItemId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string Text { get; set; } = string.Empty;
}
