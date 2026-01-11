namespace Atlas.Application.Features.Projects.TeamMembers.SetProjectTeamMembers;

public sealed record SetProjectTeamMembersCommand(Guid ProjectId, IReadOnlyList<Guid> TeamMemberIds) : IRequest<bool>;

