using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Tasks;

public sealed record TaskDto(
    Guid Id,
    string Title,
    Priority Priority,
    Atlas.Domain.Enums.TaskStatus Status,
    Guid? AssigneeId,
    Guid? ProjectId,
    Guid? RiskId,
    DateOnly? DueDate,
    IReadOnlyList<Guid> DependencyTaskIds,
    string EstimatedDurationText,
    Confidence EstimateConfidence,
    string? ActualDurationText,
    string Notes,
    DateTimeOffset LastTouchedAt);

