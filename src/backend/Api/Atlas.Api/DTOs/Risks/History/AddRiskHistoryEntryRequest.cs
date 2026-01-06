namespace Atlas.Api.DTOs.Risks.History;

public sealed record AddRiskHistoryEntryRequest(Guid RiskId, string Text);

