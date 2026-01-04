using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Projects.GetProject;

public sealed class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, Project?>
{
    private readonly IProjectRepository _projects;

    public GetProjectByIdQueryHandler(IProjectRepository projects)
    {
        _projects = projects;
    }

    public Task<Project?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        return request.IncludeDetails
            ? _projects.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            : _projects.GetByIdAsync(request.Id, cancellationToken);
    }
}

