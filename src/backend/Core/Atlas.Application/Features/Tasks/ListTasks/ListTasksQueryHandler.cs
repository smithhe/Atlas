using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Tasks.ListTasks;

public sealed class ListTasksQueryHandler : IRequestHandler<ListTasksQuery, IReadOnlyList<TaskItem>>
{
    private readonly ITaskRepository _tasks;

    public ListTasksQueryHandler(ITaskRepository tasks)
    {
        _tasks = tasks;
    }

    public Task<IReadOnlyList<TaskItem>> Handle(ListTasksQuery request, CancellationToken cancellationToken)
    {
        return _tasks.ListAsync(cancellationToken);
    }
}

