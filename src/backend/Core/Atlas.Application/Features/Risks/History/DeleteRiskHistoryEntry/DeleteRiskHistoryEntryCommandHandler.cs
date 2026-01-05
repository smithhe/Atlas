using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Risks.History.DeleteRiskHistoryEntry;

public sealed class DeleteRiskHistoryEntryCommandHandler : IRequestHandler<DeleteRiskHistoryEntryCommand, bool>
{
    private readonly IRiskRepository _risks;
    private readonly IUnitOfWork _uow;

    public DeleteRiskHistoryEntryCommandHandler(IRiskRepository risks, IUnitOfWork uow)
    {
        _risks = risks;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteRiskHistoryEntryCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var risk = await _risks.GetByIdWithDetailsAsync(request.RiskId, cancellationToken);
        if (risk is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var removed = risk.History.RemoveAll(x => x.Id == request.EntryId);
        if (removed == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        risk.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

