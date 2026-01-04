using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Projects.ListProjects;

public sealed class ListProjectsQueryHandler : IRequestHandler<ListProjectsQuery, IReadOnlyList<Project>>
{
    private readonly IProjectRepository _projects;

    public ListProjectsQueryHandler(IProjectRepository projects)
    {
        _projects = projects;
    }

    public Task<IReadOnlyList<Project>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        return _projects.ListAsync(cancellationToken);
    }
}

