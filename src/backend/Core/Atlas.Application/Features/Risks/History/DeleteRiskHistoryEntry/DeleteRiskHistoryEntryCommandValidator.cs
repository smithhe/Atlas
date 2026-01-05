namespace Atlas.Application.Features.Risks.History.DeleteRiskHistoryEntry;

public sealed class DeleteRiskHistoryEntryCommandValidator : AbstractValidator<DeleteRiskHistoryEntryCommand>
{
    public DeleteRiskHistoryEntryCommandValidator()
    {
        RuleFor(x => x.RiskId).NotEmpty();
        RuleFor(x => x.EntryId).NotEmpty();
    }
}

