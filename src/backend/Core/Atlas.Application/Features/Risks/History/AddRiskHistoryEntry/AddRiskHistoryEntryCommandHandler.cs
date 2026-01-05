using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Risks.History.AddRiskHistoryEntry;

public sealed class AddRiskHistoryEntryCommandHandler : IRequestHandler<AddRiskHistoryEntryCommand, Guid>
{
    private readonly IRiskRepository _risks;
    private readonly IUnitOfWork _uow;

    public AddRiskHistoryEntryCommandHandler(IRiskRepository risks, IUnitOfWork uow)
    {
        _risks = risks;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddRiskHistoryEntryCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var risk = await _risks.GetByIdWithDetailsAsync(request.RiskId, cancellationToken);
        if (risk is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        var entry = new RiskHistoryEntry
        {
            Id = Guid.NewGuid(),
            RiskId = risk.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            Text = request.Text
        };

        risk.History.Add(entry);
        risk.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return entry.Id;
    }
}

