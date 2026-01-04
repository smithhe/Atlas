using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Tasks.DeleteTask;

public sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, bool>
{
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _uow;

    public DeleteTaskCommandHandler(ITaskRepository tasks, IUnitOfWork uow)
    {
        _tasks = tasks;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var task = await _tasks.GetByIdAsync(request.Id, cancellationToken);
        if (task is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        _tasks.Remove(task);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

