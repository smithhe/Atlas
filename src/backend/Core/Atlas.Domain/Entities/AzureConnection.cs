using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class AzureConnection : AggregateRoot
{
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string AreaPath { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public bool IsEnabled { get; set; } = true;
}
