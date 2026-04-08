using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Risks.DeleteRisk;

public sealed class DeleteRiskCommandHandler : IRequestHandler<DeleteRiskCommand, bool>
{
    private readonly IRiskRepository _risks;
    private readonly IUnitOfWork _uow;

    public DeleteRiskCommandHandler(IRiskRepository risks, IUnitOfWork uow)
    {
        _risks = risks;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteRiskCommand request, CancellationToken cancellationToken)
    {
        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        Risk? risk = await _risks.GetByIdAsync(request.Id, cancellationToken);
        if (risk is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        _risks.Remove(risk);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

