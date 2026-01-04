namespace Atlas.Domain.Abstractions;

/// <summary>
/// Marker base class for Domain-Driven Design aggregates.
///
/// An Aggregate Root is the only entity that should be referenced/loaded directly
/// from outside the aggregate boundary. Child entities/value objects should be
/// reached and mutated through the root to preserve invariants.
///
/// This base type is intentionally small today, but it’s a useful extension point for:
/// - Domain events collection (e.g., pending events raised by the aggregate)
/// - Concurrency/version fields (typically on mutable aggregates)
/// - Shared aggregate-only helpers/invariant enforcement patterns
/// </summary>
public abstract class AggregateRoot : Entity
{
}

