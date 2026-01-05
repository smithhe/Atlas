namespace Atlas.Application.Features.Risks.History.AddRiskHistoryEntry;

public sealed class AddRiskHistoryEntryCommandValidator : AbstractValidator<AddRiskHistoryEntryCommand>
{
    public AddRiskHistoryEntryCommandValidator()
    {
        RuleFor(x => x.RiskId).NotEmpty();

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(20000);
    }
}

