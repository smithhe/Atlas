using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class AzureWorkItem : AggregateRoot
{
    public Guid AzureConnectionId { get; set; }
    public AzureConnection? AzureConnection { get; set; }

    public int WorkItemId { get; set; }
    public int Rev { get; set; }
    public DateTimeOffset ChangedDateUtc { get; set; }

    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string AreaPath { get; set; } = string.Empty;
    public string IterationPath { get; set; } = string.Empty;
    public string? AssignedToUniqueName { get; set; }
    public string Url { get; set; } = string.Empty;
}
