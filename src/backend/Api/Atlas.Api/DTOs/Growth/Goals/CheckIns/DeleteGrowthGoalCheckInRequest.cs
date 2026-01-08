namespace Atlas.Api.DTOs.Growth.Goals.CheckIns;

public sealed record DeleteGrowthGoalCheckInRequest(Guid GrowthId, Guid GoalId, Guid CheckInId);

