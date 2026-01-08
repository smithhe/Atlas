using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Growth;

public sealed record GrowthGoalDto(
    Guid Id,
    string Title,
    string Description,
    GrowthGoalStatus Status,
    string? Category,
    Priority? Priority,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    DateTimeOffset? LastUpdatedAt,
    int? ProgressPercent,
    string? Summary,
    IReadOnlyList<string> SuccessCriteria,
    IReadOnlyList<GrowthGoalActionDto> Actions,
    IReadOnlyList<GrowthGoalCheckInDto> CheckIns);

