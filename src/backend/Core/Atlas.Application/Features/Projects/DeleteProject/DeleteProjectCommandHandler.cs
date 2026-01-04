using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.Projects.DeleteProject;

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, bool>
{
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _uow;

    public DeleteProjectCommandHandler(IProjectRepository projects, IUnitOfWork uow)
    {
        _projects = projects;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var project = await _projects.GetByIdAsync(request.Id, cancellationToken);
        if (project is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        _projects.Remove(project);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

