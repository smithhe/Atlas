using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Tasks.CreateTask;

public sealed record CreateTaskCommand(
    string Title,
    Priority Priority,
    Atlas.Domain.Enums.TaskStatus Status,
    Guid? AssigneeId,
    Guid? ProjectId,
    Guid? RiskId,
    DateOnly? DueDate,
    string EstimatedDurationText,
    Confidence EstimateConfidence,
    string? ActualDurationText,
    string Notes,
    IReadOnlyList<Guid>? BlockedByTaskIds = null) : IRequest<Guid>;

