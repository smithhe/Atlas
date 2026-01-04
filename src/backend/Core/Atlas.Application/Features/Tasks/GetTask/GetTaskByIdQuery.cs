using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Tasks.GetTask;

public sealed record GetTaskByIdQuery(Guid Id, bool IncludeDetails = true) : IRequest<TaskItem?>;

