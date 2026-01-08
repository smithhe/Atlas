using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Growth;

public sealed record GrowthGoalActionDto(
    Guid Id,
    string Title,
    DateOnly? DueDate,
    GrowthGoalActionState State,
    Priority? Priority,
    string? Notes,
    string? Evidence);

