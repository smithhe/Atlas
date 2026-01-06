using Atlas.Application.Features.Tasks.ListTasks;
using Atlas.Api.DTOs.Tasks;
using Atlas.Api.Mappers;

namespace Atlas.Api.Endpoints.Tasks;

public sealed class ListTasksEndpoint : Endpoint<ListTasksRequest, IReadOnlyList<TaskDto>>
{
    private readonly IMediator _mediator;

    public ListTasksEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/tasks");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List tasks";
        });
    }

    public override async Task HandleAsync(ListTasksRequest req, CancellationToken ct)
    {
        var tasks = await _mediator.Send(new ListTasksQuery(req.Ids), ct);
        var dtos = tasks.Select(TaskMapper.ToDto).ToList();
        await Send.OkAsync(dtos, ct);
    }
}

