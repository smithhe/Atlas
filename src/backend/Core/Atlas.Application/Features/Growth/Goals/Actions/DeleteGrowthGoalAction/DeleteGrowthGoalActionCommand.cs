namespace Atlas.Application.Features.Growth.Goals.Actions.DeleteGrowthGoalAction;

public sealed record DeleteGrowthGoalActionCommand(Guid GrowthId, Guid GoalId, Guid ActionId) : IRequest<bool>;

