using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Risks.UpdateRisk;

public sealed record UpdateRiskCommand(
    Guid Id,
    string Title,
    RiskStatus Status,
    SeverityLevel Severity,
    Guid? ProjectId,
    string Description,
    string Evidence) : IRequest<bool>;

