namespace Atlas.Application.Features.Tasks.DeleteTask;

public sealed record DeleteTaskCommand(Guid Id) : IRequest<bool>;

