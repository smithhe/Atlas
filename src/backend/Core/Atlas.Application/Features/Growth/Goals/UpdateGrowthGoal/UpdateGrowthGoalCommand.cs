using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Growth.Goals.UpdateGrowthGoal;

public sealed record UpdateGrowthGoalCommand(
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
    string? SuccessCriteria) : IRequest<bool>;

