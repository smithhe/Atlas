namespace Atlas.Application.Features.Risks.History.DeleteRiskHistoryEntry;

public sealed record DeleteRiskHistoryEntryCommand(Guid RiskId, Guid EntryId) : IRequest<bool>;

