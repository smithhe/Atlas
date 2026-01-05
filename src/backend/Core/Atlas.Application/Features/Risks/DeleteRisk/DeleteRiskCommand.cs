namespace Atlas.Application.Features.Risks.DeleteRisk;

public sealed record DeleteRiskCommand(Guid Id) : IRequest<bool>;

