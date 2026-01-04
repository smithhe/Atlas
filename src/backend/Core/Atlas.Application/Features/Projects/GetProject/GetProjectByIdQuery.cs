using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Projects.GetProject;

public sealed record GetProjectByIdQuery(Guid Id, bool IncludeDetails = true) : IRequest<Project?>;

