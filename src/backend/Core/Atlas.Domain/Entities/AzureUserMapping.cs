using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class AzureUserMapping : AggregateRoot
{
    public string AzureUniqueName { get; set; } = string.Empty;
    public Guid TeamMemberId { get; set; }
    public TeamMember? TeamMember { get; set; }
    public DateTimeOffset LinkedAtUtc { get; set; }
}
