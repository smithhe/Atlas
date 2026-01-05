using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Growth.Goals.AddGrowthGoal;

public sealed class AddGrowthGoalCommandHandler : IRequestHandler<AddGrowthGoalCommand, Guid>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public AddGrowthGoalCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddGrowthGoalCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var plan = await _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        var goal = new GrowthGoal
        {
            Id = Guid.NewGuid(),
            GrowthId = plan.Id,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = request.Status,
            StartDate = request.StartDate,
            TargetDate = request.TargetDate,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            Priority = request.Priority,
            LastUpdatedAt = DateTime.UtcNow,
            Summary = string.Empty,
            ProgressPercent = null,
            SuccessCriteria = string.Empty
        };

        plan.Goals.Add(goal);

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return goal.Id;
    }
}

