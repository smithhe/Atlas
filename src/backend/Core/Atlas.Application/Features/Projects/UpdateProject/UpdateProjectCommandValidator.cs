namespace Atlas.Application.Features.Projects.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Summary)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Description)
            .MaximumLength(20000);

        When(x => x.Tags is not null, () =>
        {
            RuleForEach(x => x.Tags!)
                .NotEmpty()
                .MaximumLength(200);
        });

        When(x => x.Links is not null, () =>
        {
            RuleForEach(x => x.Links!).ChildRules(link =>
            {
                link.RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
                link.RuleFor(x => x.Url)
                    .NotEmpty()
                    .MaximumLength(2000)
                    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                    .WithMessage("Url must be a valid absolute URI.");
            });
        });
    }
}

