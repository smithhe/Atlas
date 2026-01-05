namespace Atlas.Application.Features.TeamMembers.Notes.AddTeamNote;

public sealed class AddTeamNoteCommandValidator : AbstractValidator<AddTeamNoteCommand>
{
    public AddTeamNoteCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();

        RuleFor(x => x.Title)
            .MaximumLength(500);

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(50000);
    }
}

