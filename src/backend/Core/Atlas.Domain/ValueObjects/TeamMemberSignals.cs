using Atlas.Domain.Enums;

namespace Atlas.Domain.ValueObjects;

public sealed record TeamMemberSignals
{
    public LoadSignal Load { get; init; }
    public DeliverySignal Delivery { get; init; }
    public SupportNeededSignal SupportNeeded { get; init; }
}

