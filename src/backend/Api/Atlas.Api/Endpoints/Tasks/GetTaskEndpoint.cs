using Atlas.Application.Features.Tasks.GetTask;
using Atlas.Api.DTOs.Tasks;
using Atlas.Api.Mappers;

namespace Atlas.Api.Endpoints.Tasks;

public sealed class GetTaskEndpoint : Endpoint<GetTaskRequest, TaskDto>
{
    private readonly IMediator _mediator;

    public GetTaskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/tasks/{id:guid}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a task by id";
        });
    }

    public override async Task HandleAsync(GetTaskRequest req, CancellationToken ct)
    {
        var task = await _mediator.Send(new GetTaskByIdQuery(req.Id, IncludeDetails: true), ct);
        if (task is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(TaskMapper.ToDto(task), ct);
    }
}

