using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Growth.Goals.CheckIns.UpdateGrowthGoalCheckIn;

public sealed record UpdateGrowthGoalCheckInCommand(
    Guid GrowthId,
    Guid GoalId,
    Guid CheckInId,
    DateOnly Date,
    GrowthGoalCheckInSignal Signal,
    string Note) : IRequest<bool>;

