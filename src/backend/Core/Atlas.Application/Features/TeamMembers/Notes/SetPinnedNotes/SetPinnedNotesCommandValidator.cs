namespace Atlas.Application.Features.TeamMembers.Notes.SetPinnedNotes;

public sealed class SetPinnedNotesCommandValidator : AbstractValidator<SetPinnedNotesCommand>
{
    public SetPinnedNotesCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();

        RuleForEach(x => x.NoteIdsInOrder)
            .NotEmpty();
    }
}

