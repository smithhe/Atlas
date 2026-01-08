using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Growth.Goals;

public sealed record AddGrowthGoalRequest(
    Guid GrowthId,
    string Title,
    string Description,
    GrowthGoalStatus Status,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    string? Category,
    Priority? Priority);

