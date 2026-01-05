using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Growth.Goals.AddGrowthGoal;

public sealed record AddGrowthGoalCommand(
    Guid GrowthId,
    string Title,
    string Description,
    GrowthGoalStatus Status,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    string? Category,
    Priority? Priority) : IRequest<Guid>;

