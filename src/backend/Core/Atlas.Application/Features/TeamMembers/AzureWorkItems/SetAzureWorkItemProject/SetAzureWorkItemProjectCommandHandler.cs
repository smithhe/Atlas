using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.AzureWorkItems.SetAzureWorkItemProject;

public sealed class SetAzureWorkItemProjectCommandHandler : IRequestHandler<SetAzureWorkItemProjectCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _uow;

    public SetAzureWorkItemProjectCommandHandler(
        ITeamMemberRepository team,
        IProjectRepository projects,
        IUnitOfWork uow)
    {
        _team = team;
        _projects = projects;
        _uow = uow;
    }

    public async Task<bool> Handle(SetAzureWorkItemProjectCommand request, CancellationToken cancellationToken)
    {
        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        TeamMember? member = await _team.GetByIdWithDetailsAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        AzureWorkItemLink? link = member.AzureWorkItemLinks
            .FirstOrDefault(x => x.AzureWorkItem?.WorkItemId == request.WorkItemId);
        if (link is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        if (request.ProjectId is Guid projectId)
        {
            Project? project = await _projects.GetByIdAsync(projectId, cancellationToken);
            if (project is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return false;
            }

            link.ProjectId = projectId;
        }
        else
        {
            link.ProjectId = Guid.Empty;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}
