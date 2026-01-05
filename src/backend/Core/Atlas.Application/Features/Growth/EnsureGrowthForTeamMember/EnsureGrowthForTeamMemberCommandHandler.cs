using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.EnsureGrowthForTeamMember;

public sealed class EnsureGrowthForTeamMemberCommandHandler : IRequestHandler<EnsureGrowthForTeamMemberCommand, Guid>
{
    private readonly IGrowthRepository _growth;
    private readonly IUnitOfWork _uow;

    public EnsureGrowthForTeamMemberCommandHandler(IGrowthRepository growth, IUnitOfWork uow)
    {
        _growth = growth;
        _uow = uow;
    }

    public async Task<Guid> Handle(EnsureGrowthForTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var existing = await _growth.GetByTeamMemberIdAsync(request.TeamMemberId, cancellationToken);
        if (existing is not null) return existing.Id;

        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        // Re-check inside the transaction (race safety).
        existing = await _growth.GetByTeamMemberIdAsync(request.TeamMemberId, cancellationToken);
        if (existing is not null)
        {
            await tx.CommitAsync(cancellationToken);
            return existing.Id;
        }

        var plan = new Atlas.Domain.Entities.Growth
        {
            Id = Guid.NewGuid(),
            TeamMemberId = request.TeamMemberId,
            FocusAreasMarkdown = string.Empty
        };

        await _growth.AddAsync(plan, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return plan.Id;
    }
}

