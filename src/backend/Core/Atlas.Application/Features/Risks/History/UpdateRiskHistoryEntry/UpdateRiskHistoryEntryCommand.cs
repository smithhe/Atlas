namespace Atlas.Application.Features.Risks.History.UpdateRiskHistoryEntry;

public sealed record UpdateRiskHistoryEntryCommand(Guid RiskId, Guid EntryId, string Text) : IRequest<bool>;

