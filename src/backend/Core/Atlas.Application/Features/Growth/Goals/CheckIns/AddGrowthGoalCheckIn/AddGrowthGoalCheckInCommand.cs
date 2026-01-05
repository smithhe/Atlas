using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Growth.Goals.CheckIns.AddGrowthGoalCheckIn;

public sealed record AddGrowthGoalCheckInCommand(
    Guid GrowthId,
    Guid GoalId,
    DateOnly Date,
    GrowthGoalCheckInSignal Signal,
    string Note) : IRequest<Guid>;

