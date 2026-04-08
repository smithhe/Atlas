using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Risks.History.UpdateRiskHistoryEntry;

public sealed class UpdateRiskHistoryEntryCommandHandler : IRequestHandler<UpdateRiskHistoryEntryCommand, bool>
{
    private readonly IRiskRepository _risks;
    private readonly IUnitOfWork _uow;

    public UpdateRiskHistoryEntryCommandHandler(IRiskRepository risks, IUnitOfWork uow)
    {
        _risks = risks;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateRiskHistoryEntryCommand request, CancellationToken cancellationToken)
    {
        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        Risk? risk = await _risks.GetByIdWithDetailsAsync(request.RiskId, cancellationToken);
        if (risk is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        RiskHistoryEntry? entry = risk.History.FirstOrDefault(x => x.Id == request.EntryId);
        if (entry is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        entry.Text = request.Text;
        risk.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

