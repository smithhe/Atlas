using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Tasks.UpdateTask;

public sealed record UpdateTaskCommand(
    Guid Id,
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
    IReadOnlyList<Guid>? BlockedByTaskIds = null) : IRequest<bool>;

