using Atlas.Domain.Enums;

namespace Atlas.Application.Features.TeamMembers.Signals.UpdateTeamMemberSignals;

public sealed record UpdateTeamMemberSignalsCommand(
    Guid TeamMemberId,
    LoadSignal Load,
    DeliverySignal Delivery,
    SupportNeededSignal SupportNeeded) : IRequest<bool>;

