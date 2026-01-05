using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.Goals.CheckIns.UpdateGrowthGoalCheckIn;

public sealed class UpdateGrowthGoalCheckInCommandHandler : IRequestHandler<UpdateGrowthGoalCheckInCommand, bool>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public UpdateGrowthGoalCheckInCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateGrowthGoalCheckInCommand request, CancellationToken cancellationToken)
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

        var checkIn = goal.CheckIns.FirstOrDefault(x => x.Id == request.CheckInId);
        if (checkIn is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        checkIn.Date = request.Date;
        checkIn.Signal = request.Signal;
        checkIn.Note = request.Note.Trim();
        goal.LastUpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

