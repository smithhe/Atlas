using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Growth.GetGrowth;

public sealed class GetGrowthByTeamMemberIdQueryHandler
    : IRequestHandler<GetGrowthByTeamMemberIdQuery, Atlas.Domain.Entities.Growth?>
{
    private readonly IGrowthRepository _growth;

    public GetGrowthByTeamMemberIdQueryHandler(IGrowthRepository growth)
    {
        _growth = growth;
    }

    public Task<Atlas.Domain.Entities.Growth?> Handle(GetGrowthByTeamMemberIdQuery request, CancellationToken cancellationToken)
    {
        return _growth.GetByTeamMemberIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
    }
}

