namespace Atlas.Application.Features.Risks.History.AddRiskHistoryEntry;

public sealed record AddRiskHistoryEntryCommand(Guid RiskId, string Text) : IRequest<Guid>;

