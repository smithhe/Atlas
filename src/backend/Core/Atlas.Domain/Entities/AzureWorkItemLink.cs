using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class AzureWorkItemLink : AggregateRoot
{
    public Guid AzureWorkItemId { get; set; }
    public AzureWorkItem? AzureWorkItem { get; set; }

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? TeamMemberId { get; set; }
    public TeamMember? TeamMember { get; set; }

    public DateTimeOffset LinkedAtUtc { get; set; }
}
