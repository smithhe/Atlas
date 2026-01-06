using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Risks;

public sealed record RiskListItemDto(
    Guid Id,
    string Title,
    RiskStatus Status,
    SeverityLevel Severity,
    Guid? ProjectId,
    DateTimeOffset LastUpdatedAt);