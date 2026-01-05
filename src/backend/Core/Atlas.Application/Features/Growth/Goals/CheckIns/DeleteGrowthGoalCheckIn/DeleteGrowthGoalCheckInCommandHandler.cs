using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.Goals.CheckIns.DeleteGrowthGoalCheckIn;

public sealed class DeleteGrowthGoalCheckInCommandHandler : IRequestHandler<DeleteGrowthGoalCheckInCommand, bool>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public DeleteGrowthGoalCheckInCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteGrowthGoalCheckInCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var plan = await _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var goal = plan.Goals.FirstOrDefault(x => x.Id == request.GoalId);
        if (goal is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var removed = goal.CheckIns.RemoveAll(x => x.Id == request.CheckInId);
        if (removed == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        goal.LastUpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

