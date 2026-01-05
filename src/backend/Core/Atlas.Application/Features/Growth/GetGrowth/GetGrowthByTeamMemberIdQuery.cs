namespace Atlas.Application.Features.Growth.GetGrowth;

public sealed record GetGrowthByTeamMemberIdQuery(Guid TeamMemberId) : IRequest<Atlas.Domain.Entities.Growth?>;

