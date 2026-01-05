namespace Atlas.Application.Features.TeamMembers.DeleteTeamMember;

public sealed record DeleteTeamMemberCommand(Guid Id) : IRequest<bool>;

