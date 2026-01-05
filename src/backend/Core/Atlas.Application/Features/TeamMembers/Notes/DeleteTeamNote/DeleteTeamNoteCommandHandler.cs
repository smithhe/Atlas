using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.TeamMembers.Notes.DeleteTeamNote;

public sealed class DeleteTeamNoteCommandHandler : IRequestHandler<DeleteTeamNoteCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public DeleteTeamNoteCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteTeamNoteCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var removed = member.Notes.RemoveAll(n => n.Id == request.NoteId);
        if (removed == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        // Re-pack pinned ordering if needed.
        var pinned = member.Notes.Where(n => n.PinnedOrder is not null).OrderBy(n => n.PinnedOrder).ToList();
        for (var i = 0; i < pinned.Count; i++)
        {
            pinned[i].PinnedOrder = i;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

