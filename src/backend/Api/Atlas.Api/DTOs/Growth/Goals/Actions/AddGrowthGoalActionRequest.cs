using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Growth.Goals.Actions;

public sealed record AddGrowthGoalActionRequest(
    Guid GrowthId,
    Guid GoalId,
    string Title,
    GrowthGoalActionState State,
    DateOnly? DueDate,
    Priority? Priority,
    string? Notes,
    string? Evidence);

