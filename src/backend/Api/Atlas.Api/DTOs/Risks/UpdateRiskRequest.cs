using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Risks;

public sealed record UpdateRiskRequest(
    string Title,
    RiskStatus Status,
    SeverityLevel Severity,
    Guid? ProjectId,
    string Description,
    string Evidence);

