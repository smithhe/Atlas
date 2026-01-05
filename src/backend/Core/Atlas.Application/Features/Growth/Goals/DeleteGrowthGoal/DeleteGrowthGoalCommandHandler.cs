using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.Goals.DeleteGrowthGoal;

public sealed class DeleteGrowthGoalCommandHandler : IRequestHandler<DeleteGrowthGoalCommand, bool>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public DeleteGrowthGoalCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteGrowthGoalCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var plan = await _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var removed = plan.Goals.RemoveAll(x => x.Id == request.GoalId);
        if (removed == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

