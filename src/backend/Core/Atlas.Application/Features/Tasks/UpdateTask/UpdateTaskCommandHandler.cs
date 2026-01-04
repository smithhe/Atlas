using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Tasks.UpdateTask;

public sealed class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, bool>
{
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _uow;

    public UpdateTaskCommandHandler(ITaskRepository tasks, IUnitOfWork uow)
    {
        _tasks = tasks;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var task = await _tasks.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (task is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        task.Title = request.Title;
        task.Priority = request.Priority;
        task.Status = request.Status;
        task.AssigneeId = request.AssigneeId;
        task.ProjectId = request.ProjectId;
        task.RiskId = request.RiskId;
        task.DueDate = request.DueDate;
        task.EstimatedDurationText = request.EstimatedDurationText;
        task.EstimateConfidence = request.EstimateConfidence;
        task.ActualDurationText = request.ActualDurationText;
        task.Notes = request.Notes;

        var desiredBlockerIds = (request.BlockedByTaskIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty && id != task.Id)
            .Distinct()
            .ToHashSet();

        task.BlockedBy.RemoveAll(d => !desiredBlockerIds.Contains(d.BlockerTaskId));
        foreach (var blockerId in desiredBlockerIds)
        {
            var exists = task.BlockedBy.Any(d => d.BlockerTaskId == blockerId);
            if (exists)
            {
                continue;
            }

            task.BlockedBy.Add(new TaskDependency
            {
                Id = Guid.NewGuid(),
                DependentTaskId = task.Id,
                BlockerTaskId = blockerId
            });
        }

        task.LastTouchedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

