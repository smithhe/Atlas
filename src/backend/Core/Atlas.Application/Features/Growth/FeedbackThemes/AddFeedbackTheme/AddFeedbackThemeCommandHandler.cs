using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Growth.FeedbackThemes.AddFeedbackTheme;

public sealed class AddFeedbackThemeCommandHandler : IRequestHandler<AddFeedbackThemeCommand, Guid>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public AddFeedbackThemeCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddFeedbackThemeCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var plan = await _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
        if (plan is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        var theme = new GrowthFeedbackTheme
        {
            Id = Guid.NewGuid(),
            GrowthId = plan.Id,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ObservedSinceLabel = string.IsNullOrWhiteSpace(request.ObservedSinceLabel) ? null : request.ObservedSinceLabel.Trim()
        };

        plan.FeedbackThemes.Add(theme);

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return theme.Id;
    }
}

