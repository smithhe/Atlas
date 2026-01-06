namespace Atlas.Api.DTOs.Risks.History;

public sealed record UpdateRiskHistoryEntryRequest(Guid RiskId, Guid EntryId, string Text);

