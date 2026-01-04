using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class GrowthFeedbackTheme : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ObservedSinceLabel { get; set; }
}

