namespace Atlas.Application.Features.TeamMembers.Notes.DeleteTeamNote;

public sealed class DeleteTeamNoteCommandValidator : AbstractValidator<DeleteTeamNoteCommand>
{
    public DeleteTeamNoteCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();
        RuleFor(x => x.NoteId).NotEmpty();
    }
}

