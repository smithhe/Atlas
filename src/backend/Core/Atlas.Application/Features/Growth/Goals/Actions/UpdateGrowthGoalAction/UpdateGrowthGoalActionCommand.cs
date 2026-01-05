using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Growth.Goals.Actions.UpdateGrowthGoalAction;

public sealed record UpdateGrowthGoalActionCommand(
    Guid GrowthId,
    Guid GoalId,
    Guid ActionId,
    string Title,
    GrowthGoalActionState State,
    DateOnly? DueDate,
    Priority? Priority,
    string? Notes,
    string? Evidence) : IRequest<bool>;

