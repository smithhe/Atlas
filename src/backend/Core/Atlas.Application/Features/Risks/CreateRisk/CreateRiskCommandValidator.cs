namespace Atlas.Application.Features.Risks.CreateRisk;

public sealed class CreateRiskCommandValidator : AbstractValidator<CreateRiskCommand>
{
    public CreateRiskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Description)
            .NotNull()
            .MaximumLength(20000);

        RuleFor(x => x.Evidence)
            .NotNull()
            .MaximumLength(20000);
    }
}

