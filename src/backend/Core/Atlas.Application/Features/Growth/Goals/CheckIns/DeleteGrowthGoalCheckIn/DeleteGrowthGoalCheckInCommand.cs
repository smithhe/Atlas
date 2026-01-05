namespace Atlas.Application.Features.Growth.Goals.CheckIns.DeleteGrowthGoalCheckIn;

public sealed record DeleteGrowthGoalCheckInCommand(Guid GrowthId, Guid GoalId, Guid CheckInId) : IRequest<bool>;

