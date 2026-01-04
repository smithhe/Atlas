using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

/// <summary>
/// Represents a selectable Product Owner option.
///
/// Note: <see cref="Project.ProductOwnerId"/> can reference this entity's <see cref="Entity.Id"/>
/// once persistence (Infrastructure) is introduced.
/// </summary>
public sealed class ProductOwner : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
}

