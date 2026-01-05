using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.GetGrowth;

public sealed class GetGrowthByIdQueryHandler : IRequestHandler<GetGrowthByIdQuery, Atlas.Domain.Entities.Growth?>
{
    private readonly IGrowthRepository _growth;

    public GetGrowthByIdQueryHandler(IGrowthRepository growth)
    {
        _growth = growth;
    }

    public Task<Atlas.Domain.Entities.Growth?> Handle(GetGrowthByIdQuery request, CancellationToken cancellationToken)
    {
        return _growth.GetByIdWithDetailsAsync(request.GrowthId, cancellationToken);
    }
}

