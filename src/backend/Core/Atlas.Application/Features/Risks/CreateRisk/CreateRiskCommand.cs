using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Risks.CreateRisk;

public sealed record CreateRiskCommand(
    string Title,
    RiskStatus Status,
    SeverityLevel Severity,
    Guid? ProjectId,
    string Description,
    string Evidence) : IRequest<Guid>;

