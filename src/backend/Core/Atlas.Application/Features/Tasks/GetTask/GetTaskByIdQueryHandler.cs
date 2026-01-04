using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Tasks.GetTask;

public sealed class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskItem?>
{
    private readonly ITaskRepository _tasks;

    public GetTaskByIdQueryHandler(ITaskRepository tasks)
    {
        _tasks = tasks;
    }

    public Task<TaskItem?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        return request.IncludeDetails
            ? _tasks.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            : _tasks.GetByIdAsync(request.Id, cancellationToken);
    }
}

