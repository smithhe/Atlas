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
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var plan = await _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        var goal = plan.Goals.FirstOrDefault(x => x.Id == request.GoalId);
        if (goal is null)
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

        goal.CheckIns.Add(checkIn);
        goal.LastUpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return checkIn.Id;
    }
}

