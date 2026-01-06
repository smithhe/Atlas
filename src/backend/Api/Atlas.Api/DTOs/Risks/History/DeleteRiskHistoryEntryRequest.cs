namespace Atlas.Api.DTOs.Risks.History;

public sealed record DeleteRiskHistoryEntryRequest(Guid RiskId, Guid EntryId);

