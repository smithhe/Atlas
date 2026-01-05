namespace Atlas.Application.Features.Growth.Goals.DeleteGrowthGoal;

public sealed record DeleteGrowthGoalCommand(Guid GrowthId, Guid GoalId) : IRequest<bool>;

