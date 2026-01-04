using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;

namespace Atlas.Domain.Entities;

public sealed class GrowthGoalAction : Entity
{
    public string Title { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public GrowthGoalActionState State { get; set; }
    public Priority? Priority { get; set; }
    public string? Notes { get; set; }
    public string? Evidence { get; set; }
}

