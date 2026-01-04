using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Tasks.ListTasks;

public sealed record ListTasksQuery : IRequest<IReadOnlyList<TaskItem>>;

