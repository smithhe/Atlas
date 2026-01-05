using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Growth.Goals.Actions.AddGrowthGoalAction;

public sealed record AddGrowthGoalActionCommand(
    Guid GrowthId,
    Guid GoalId,
    string Title,
    GrowthGoalActionState State,
    DateOnly? DueDate,
    Priority? Priority,
    string? Notes,
    string? Evidence) : IRequest<Guid>;

