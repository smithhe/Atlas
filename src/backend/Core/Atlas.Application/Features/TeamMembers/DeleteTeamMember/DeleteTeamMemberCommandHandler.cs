using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.TeamMembers.DeleteTeamMember;

public sealed class DeleteTeamMemberCommandHandler : IRequestHandler<DeleteTeamMemberCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public DeleteTeamMemberCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteTeamMemberCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdAsync(request.Id, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        _team.Remove(member);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

