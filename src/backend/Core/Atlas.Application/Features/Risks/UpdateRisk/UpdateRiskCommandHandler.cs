using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Risks.UpdateRisk;

public sealed class UpdateRiskCommandHandler : IRequestHandler<UpdateRiskCommand, bool>
{
    private readonly IRiskRepository _risks;
    private readonly IUnitOfWork _uow;

    public UpdateRiskCommandHandler(IRiskRepository risks, IUnitOfWork uow)
    {
        _risks = risks;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateRiskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var risk = await _risks.GetByIdAsync(request.Id, cancellationToken);
        if (risk is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        risk.Title = request.Title;
        risk.Status = request.Status;
        risk.Severity = request.Severity;
        risk.ProjectId = request.ProjectId;
        risk.Description = request.Description;
        risk.Evidence = request.Evidence;
        risk.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

