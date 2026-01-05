using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.FeedbackThemes.DeleteFeedbackTheme;

public sealed class DeleteFeedbackThemeCommandHandler : IRequestHandler<DeleteFeedbackThemeCommand, bool>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public DeleteFeedbackThemeCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteFeedbackThemeCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var plan = await _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var removed = plan.FeedbackThemes.RemoveAll(x => x.Id == request.ThemeId);
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

