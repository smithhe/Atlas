namespace Atlas.Application.Features.Projects.DeleteProject;

public sealed record DeleteProjectCommand(Guid Id) : IRequest<bool>;

