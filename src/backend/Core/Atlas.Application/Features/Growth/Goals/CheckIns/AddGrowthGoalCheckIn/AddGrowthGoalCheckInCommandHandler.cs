using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Growth.Goals.CheckIns.AddGrowthGoalCheckIn;

public sealed class AddGrowthGoalCheckInCommandHandler : IRequestHandler<AddGrowthGoalCheckInCommand, Guid>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public AddGrowthGoalCheckInCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddGrowthGoalCheckInCommand request, CancellationToken cancellationToken)
    {
        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        Domain.Entities.Growth? plan = await _growth.GetByIdAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        GrowthGoal? goal = await _growth.GetGoalByIdAsync(request.GoalId, cancellationToken);
        if (goal is null || goal.GrowthId != plan.Id)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        var checkIn = new GrowthGoalCheckIn
        {
            Id = Guid.NewGuid(),
            GrowthGoalId = goal.Id,
            Date = request.Date,
            Signal = request.Signal,
            Note = request.Note.Trim()
        };

        goal.LastUpdatedAt = DateTime.UtcNow;
        await _growth.AddGoalCheckInAsync(checkIn, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return checkIn.Id;
    }
}

