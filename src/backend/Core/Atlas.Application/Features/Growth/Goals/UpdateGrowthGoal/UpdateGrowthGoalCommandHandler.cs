using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.Goals.UpdateGrowthGoal;

public sealed class UpdateGrowthGoalCommandHandler : IRequestHandler<UpdateGrowthGoalCommand, bool>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public UpdateGrowthGoalCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateGrowthGoalCommand request, CancellationToken cancellationToken)
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

        goal.Title = request.Title.Trim();
        goal.Description = request.Description.Trim();
        goal.Status = request.Status;
        goal.StartDate = request.StartDate;
        goal.TargetDate = request.TargetDate;
        goal.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        goal.Priority = request.Priority;
        goal.ProgressPercent = request.ProgressPercent;
        goal.Summary = request.Summary?.Trim() ?? string.Empty;
        goal.SuccessCriteria = request.SuccessCriteria?.Trim() ?? string.Empty;
        goal.LastUpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

