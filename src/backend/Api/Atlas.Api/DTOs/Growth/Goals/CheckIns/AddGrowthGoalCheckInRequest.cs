using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Growth.Goals.CheckIns;

public sealed record AddGrowthGoalCheckInRequest(
    Guid GrowthId,
    Guid GoalId,
    DateOnly Date,
    GrowthGoalCheckInSignal Signal,
    string Note);

