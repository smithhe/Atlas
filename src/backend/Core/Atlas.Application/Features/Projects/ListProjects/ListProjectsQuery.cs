using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Projects.ListProjects;

public sealed record ListProjectsQuery : IRequest<IReadOnlyList<Project>>;

