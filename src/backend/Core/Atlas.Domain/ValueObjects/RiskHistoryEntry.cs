using Atlas.Domain.Abstractions;

namespace Atlas.Domain.ValueObjects;

public sealed class RiskHistoryEntry : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public string Text { get; set; } = string.Empty;
}

