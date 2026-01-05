using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.UpdateFocusAreas;

public sealed class UpdateGrowthFocusAreasCommandHandler : IRequestHandler<UpdateGrowthFocusAreasCommand, bool>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public UpdateGrowthFocusAreasCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateGrowthFocusAreasCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var plan = await _growth.GetByIdAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        plan.FocusAreasMarkdown = request.FocusAreasMarkdown;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

