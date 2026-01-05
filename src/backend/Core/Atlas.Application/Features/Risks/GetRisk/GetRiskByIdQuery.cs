using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Risks.GetRisk;

public sealed record GetRiskByIdQuery(Guid Id, bool IncludeDetails = true) : IRequest<Risk?>;

