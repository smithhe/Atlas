using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.TeamMembers.Notes.SetPinnedNotes;

public sealed class SetPinnedNotesCommandHandler : IRequestHandler<SetPinnedNotesCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public SetPinnedNotesCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<bool> Handle(SetPinnedNotesCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var order = request.NoteIdsInOrder
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Select((id, idx) => new { id, idx })
            .ToDictionary(x => x.id, x => x.idx);

        foreach (var note in member.Notes)
        {
            note.PinnedOrder = order.TryGetValue(note.Id, out var idx) ? idx : null;
        }

        // Validate that all requested IDs exist for this member.
        if (order.Count != request.NoteIdsInOrder.Distinct().Count(id => id != Guid.Empty))
        {
            // unreachable, but keep intent explicit
        }

        var missing = order.Keys.Except(member.Notes.Select(n => n.Id)).ToList();
        if (missing.Count > 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

