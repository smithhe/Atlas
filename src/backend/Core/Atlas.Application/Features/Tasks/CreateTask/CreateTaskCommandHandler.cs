using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Tasks.CreateTask;

public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Guid>
{
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _uow;

    public CreateTaskCommandHandler(ITaskRepository tasks, IUnitOfWork uow)
    {
        _tasks = tasks;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Priority = request.Priority,
            Status = request.Status,
            AssigneeId = request.AssigneeId,
            ProjectId = request.ProjectId,
            RiskId = request.RiskId,
            DueDate = request.DueDate,
            EstimatedDurationText = request.EstimatedDurationText,
            EstimateConfidence = request.EstimateConfidence,
            ActualDurationText = request.ActualDurationText,
            Notes = request.Notes,
            LastTouchedAt = DateTimeOffset.UtcNow
        };

        var blockerIds = (request.BlockedByTaskIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        foreach (var blockerId in blockerIds)
        {
            var exists = await _tasks.ExistsAsync(blockerId, cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException($"Blocked-by task '{blockerId}' was not found.");
            }
        }

        task.BlockedBy.AddRange(blockerIds.Select(id => new TaskDependency
        {
            Id = Guid.NewGuid(),
            DependentTaskId = task.Id,
            BlockerTaskId = id
        }));

        await _tasks.AddAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return task.Id;
    }
}

