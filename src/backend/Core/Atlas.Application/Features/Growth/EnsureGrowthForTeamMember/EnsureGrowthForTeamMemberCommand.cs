namespace Atlas.Application.Features.Growth.EnsureGrowthForTeamMember;

public sealed record EnsureGrowthForTeamMemberCommand(Guid TeamMemberId) : IRequest<Guid>;

