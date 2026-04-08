using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

/// <summary>
/// Links an Azure identity (UniqueName) to a local <see cref="ProductOwner"/>.
///
/// Intentionally modeled as many-to-one from mapping -> ProductOwner:
/// - AzureUniqueName remains unique, so an Azure user maps at most once.
/// - ProductOwner can be shared by multiple mappings when import name-dedupe
///   collapses distinct Azure identities to the same ProductOwner name.
/// </summary>
public sealed class AzureProductOwnerMapping : AggregateRoot
{
    public string AzureUniqueName { get; set; } = string.Empty;
    public Guid ProductOwnerId { get; set; }
    public ProductOwner? ProductOwner { get; set; }
    public DateTimeOffset LinkedAtUtc { get; set; }
}
