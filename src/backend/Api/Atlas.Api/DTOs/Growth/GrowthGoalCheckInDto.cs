using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Growth;

public sealed record GrowthGoalCheckInDto(
    Guid Id,
    DateOnly Date,
    GrowthGoalCheckInSignal Signal,
    string Note);

