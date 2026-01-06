namespace Atlas.Api.DTOs.Risks;

public sealed record RiskHistoryEntryDto(Guid Id, string Text, DateTimeOffset CreatedAt);