namespace Atlas.Application.Features.Growth.GetGrowth;

public sealed record GetGrowthByIdQuery(Guid GrowthId) : IRequest<Atlas.Domain.Entities.Growth?>;

