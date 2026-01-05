using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Growth.Goals.Actions.AddGrowthGoalAction;

public sealed class AddGrowthGoalActionCommandHandler : IRequestHandler<AddGrowthGoalActionCommand, Guid>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public AddGrowthGoalActionCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddGrowthGoalActionCommand request, CancellationToken cancellationToken)
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

        var action = new GrowthGoalAction
        {
            Id = Guid.NewGuid(),
            GrowthGoalId = goal.Id,
            Title = request.Title.Trim(),
            State = request.State,
            DueDate = request.DueDate,
            Priority = request.Priority,
            Notes = request.Notes?.Trim() ?? string.Empty,
            Evidence = request.Evidence?.Trim() ?? string.Empty
        };

        goal.Actions.Add(action);
        goal.LastUpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return action.Id;
    }
}

