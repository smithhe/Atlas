using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;

namespace Atlas.Domain.Entities;

public sealed class GrowthGoalCheckIn : Entity
{
    public DateOnly Date { get; set; }
    public GrowthGoalCheckInSignal Signal { get; set; }
    public string Note { get; set; } = string.Empty;
}

