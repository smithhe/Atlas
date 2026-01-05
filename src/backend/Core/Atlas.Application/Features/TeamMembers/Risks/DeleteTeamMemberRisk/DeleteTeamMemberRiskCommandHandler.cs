using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.TeamMembers.Risks.DeleteTeamMemberRisk;

public sealed class DeleteTeamMemberRiskCommandHandler : IRequestHandler<DeleteTeamMemberRiskCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public DeleteTeamMemberRiskCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteTeamMemberRiskCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var removed = member.Risks.RemoveAll(r => r.Id == request.TeamMemberRiskId);
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

