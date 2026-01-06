using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Tasks.ListTasks;

public sealed record ListTasksQuery(IReadOnlyList<Guid>? Ids = null) : IRequest<IReadOnlyList<TaskItem>>;

