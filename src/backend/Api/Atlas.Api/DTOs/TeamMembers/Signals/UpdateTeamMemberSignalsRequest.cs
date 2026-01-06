using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.TeamMembers.Signals;

public sealed record UpdateTeamMemberSignalsRequest(
    Guid TeamMemberId,
    LoadSignal Load,
    DeliverySignal Delivery,
    SupportNeededSignal SupportNeeded);

