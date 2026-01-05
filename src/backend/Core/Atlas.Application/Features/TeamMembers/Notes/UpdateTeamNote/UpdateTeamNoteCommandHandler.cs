using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.TeamMembers.Notes.UpdateTeamNote;

public sealed class UpdateTeamNoteCommandHandler : IRequestHandler<UpdateTeamNoteCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public UpdateTeamNoteCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateTeamNoteCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var note = member.Notes.FirstOrDefault(n => n.Id == request.NoteId);
        if (note is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        note.Type = request.Type;
        note.Title = request.Title;
        note.Text = request.Text;
        note.PinnedOrder = request.PinnedOrder;
        note.LastModifiedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

