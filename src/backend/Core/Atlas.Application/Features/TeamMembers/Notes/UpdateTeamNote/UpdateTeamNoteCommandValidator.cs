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

        RuleFor(x => x.AdoWorkItemId)
            .MaximumLength(64);

        RuleFor(x => x.PrUrl)
            .MaximumLength(2000)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.PrUrl))
            .WithMessage("PrUrl must be a valid absolute URI.");
    }
}

