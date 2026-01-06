using Atlas.Api.DTOs.Tasks;
using Atlas.Domain.Entities;

namespace Atlas.Api.Mappers;

internal static class TaskMapper
{
    public static TaskDto ToDto(TaskItem task)
    {
        var dependencyIds = task.BlockedBy
            .Select(d => d.BlockerTaskId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        return new TaskDto(
            task.Id,
            task.Title,
            task.Priority,
            task.Status,
            task.AssigneeId,
            task.ProjectId,
            task.RiskId,
            task.DueDate,
            dependencyIds,
            task.EstimatedDurationText,
            task.EstimateConfidence,
            task.ActualDurationText,
            task.Notes,
            task.LastTouchedAt);
    }
}

