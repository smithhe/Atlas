using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.FeedbackThemes.UpdateFeedbackTheme;

public sealed class UpdateFeedbackThemeCommandHandler : IRequestHandler<UpdateFeedbackThemeCommand, bool>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public UpdateFeedbackThemeCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateFeedbackThemeCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var plan = await _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var theme = plan.FeedbackThemes.FirstOrDefault(x => x.Id == request.ThemeId);
        if (theme is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        theme.Title = request.Title.Trim();
        theme.Description = request.Description.Trim();
        theme.ObservedSinceLabel = string.IsNullOrWhiteSpace(request.ObservedSinceLabel) ? null : request.ObservedSinceLabel.Trim();

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

