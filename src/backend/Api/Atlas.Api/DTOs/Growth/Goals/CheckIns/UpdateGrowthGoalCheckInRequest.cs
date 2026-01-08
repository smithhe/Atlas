using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Growth.Goals.CheckIns;

public sealed record UpdateGrowthGoalCheckInRequest(
    Guid GrowthId,
    Guid GoalId,
    Guid CheckInId,
    DateOnly Date,
    GrowthGoalCheckInSignal Signal,
    string Note);

