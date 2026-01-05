namespace Atlas.Application.Features.TeamMembers.Notes.UpdateTeamNote;

public sealed class UpdateTeamNoteCommandValidator : AbstractValidator<UpdateTeamNoteCommand>
{
    public UpdateTeamNoteCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();
        RuleFor(x => x.NoteId).NotEmpty();

        RuleFor(x => x.Title)
            .MaximumLength(500);

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(50000);
    }
}

