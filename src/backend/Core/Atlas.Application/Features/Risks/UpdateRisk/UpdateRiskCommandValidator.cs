namespace Atlas.Application.Features.Risks.UpdateRisk;

public sealed class UpdateRiskCommandValidator : AbstractValidator<UpdateRiskCommand>
{
    public UpdateRiskCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

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

