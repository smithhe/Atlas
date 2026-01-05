using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Risks.ListRisks;

public sealed record ListRisksQuery : IRequest<IReadOnlyList<Risk>>;

