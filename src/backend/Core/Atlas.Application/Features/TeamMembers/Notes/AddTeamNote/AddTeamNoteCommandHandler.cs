using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.Notes.AddTeamNote;

public sealed class AddTeamNoteCommandHandler : IRequestHandler<AddTeamNoteCommand, Guid>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public AddTeamNoteCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddTeamNoteCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return Guid.Empty;
        }

        var note = new TeamNote
        {
            Id = Guid.NewGuid(),
            TeamMemberId = member.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            Type = request.Type,
            Title = request.Title,
            Text = request.Text
        };

        member.Notes.Add(note);

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return note.Id;
    }
}

