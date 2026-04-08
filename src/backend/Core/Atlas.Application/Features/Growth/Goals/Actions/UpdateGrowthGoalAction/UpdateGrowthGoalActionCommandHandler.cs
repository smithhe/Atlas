using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Growth.Goals.Actions.UpdateGrowthGoalAction;

public sealed class UpdateGrowthGoalActionCommandHandler : IRequestHandler<UpdateGrowthGoalActionCommand, bool>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public UpdateGrowthGoalActionCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateGrowthGoalActionCommand request, CancellationToken cancellationToken)
    {
        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        Domain.Entities.Growth? plan = await _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        GrowthGoal? goal = plan.Goals.FirstOrDefault(x => x.Id == request.GoalId);
        if (goal is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        GrowthGoalAction? action = goal.Actions.FirstOrDefault(x => x.Id == request.ActionId);
        if (action is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        action.Title = request.Title.Trim();
        action.State = request.State;
        action.DueDate = request.DueDate;
        action.Priority = request.Priority;
        action.Notes = request.Notes?.Trim() ?? string.Empty;
        action.Evidence = request.Evidence?.Trim() ?? string.Empty;

        goal.LastUpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

