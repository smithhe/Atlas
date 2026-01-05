using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Risks.CreateRisk;

public sealed class CreateRiskCommandHandler : IRequestHandler<CreateRiskCommand, Guid>
{
    private readonly IRiskRepository _risks;
    private readonly IUnitOfWork _uow;

    public CreateRiskCommandHandler(IRiskRepository risks, IUnitOfWork uow)
    {
        _risks = risks;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateRiskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var risk = new Risk
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Status = request.Status,
            Severity = request.Severity,
            ProjectId = request.ProjectId,
            Description = request.Description,
            Evidence = request.Evidence,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        await _risks.AddAsync(risk, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return risk.Id;
    }
}

