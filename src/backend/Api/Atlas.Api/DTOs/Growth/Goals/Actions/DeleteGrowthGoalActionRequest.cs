namespace Atlas.Api.DTOs.Growth.Goals.Actions;

public sealed record DeleteGrowthGoalActionRequest(Guid GrowthId, Guid GoalId, Guid ActionId);

