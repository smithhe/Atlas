namespace Atlas.Application.Features.Risks.History.UpdateRiskHistoryEntry;

public sealed class UpdateRiskHistoryEntryCommandValidator : AbstractValidator<UpdateRiskHistoryEntryCommand>
{
    public UpdateRiskHistoryEntryCommandValidator()
    {
        RuleFor(x => x.RiskId).NotEmpty();
        RuleFor(x => x.EntryId).NotEmpty();

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(20000);
    }
}

