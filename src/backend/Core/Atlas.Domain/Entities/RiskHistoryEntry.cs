using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

/// <summary>
/// An append-only history entry for a <see cref="Risk"/>.
/// </summary>
public sealed class RiskHistoryEntry : Entity
{
    public Guid RiskId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string Text { get; set; } = string.Empty;
}

