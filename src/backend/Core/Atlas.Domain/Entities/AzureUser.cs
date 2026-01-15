using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class AzureUser : AggregateRoot
{
    public string DisplayName { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string? Descriptor { get; set; }
    public bool IsActive { get; set; } = true;
}
