using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Growth.Goals.Actions;

public sealed record UpdateGrowthGoalActionRequest(
    Guid GrowthId,
    Guid GoalId,
    Guid ActionId,
    string Title,
    GrowthGoalActionState State,
    DateOnly? DueDate,
    Priority? Priority,
    string? Notes,
    string? Evidence);

