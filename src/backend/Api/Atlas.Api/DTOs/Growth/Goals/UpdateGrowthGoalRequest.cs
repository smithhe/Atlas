using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Growth.Goals;

public sealed record UpdateGrowthGoalRequest(
    Guid GrowthId,
    Guid GoalId,
    string Title,
    string Description,
    GrowthGoalStatus Status,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    string? Category,
    Priority? Priority,
    int? ProgressPercent,
    string? Summary,
    IReadOnlyList<string>? SuccessCriteria);

