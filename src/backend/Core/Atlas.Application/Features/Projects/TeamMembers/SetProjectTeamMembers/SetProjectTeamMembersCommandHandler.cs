using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Projects.TeamMembers.SetProjectTeamMembers;

public sealed class SetProjectTeamMembersCommandHandler : IRequestHandler<SetProjectTeamMembersCommand, bool>
{
    private readonly IProjectRepository _projects;
    private readonly ITeamMemberRepository _teamMembers;
    private readonly IUnitOfWork _uow;

    public SetProjectTeamMembersCommandHandler(IProjectRepository projects, ITeamMemberRepository teamMembers, IUnitOfWork uow)
    {
        _projects = projects;
        _teamMembers = teamMembers;
        _uow = uow;
    }

    public async Task<bool> Handle(SetProjectTeamMembersCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var project = await _projects.GetByIdWithDetailsAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var desiredIds = (request.TeamMemberIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        foreach (var memberId in desiredIds)
        {
            var exists = await _teamMembers.ExistsAsync(memberId, cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException($"Team member '{memberId}' was not found.");
            }
        }

        project.TeamMembers.RemoveAll(x => !desiredIds.Contains(x.TeamMemberId));

        foreach (var memberId in desiredIds)
        {
            var exists = project.TeamMembers.Any(x => x.TeamMemberId == memberId);
            if (exists) continue;

            project.TeamMembers.Add(new ProjectTeamMember
            {
                ProjectId = project.Id,
                TeamMemberId = memberId
            });
        }

        project.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

