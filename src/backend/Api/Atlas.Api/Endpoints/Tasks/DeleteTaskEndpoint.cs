using Atlas.Application.Features.Tasks.DeleteTask;
using Atlas.Api.DTOs.Tasks;

namespace Atlas.Api.Endpoints.Tasks;

public sealed class DeleteTaskEndpoint : Endpoint<DeleteTaskRequest>
{
    private readonly IMediator _mediator;

    public DeleteTaskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/tasks/{id:guid}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Delete a task";
        });
    }

    public override async Task HandleAsync(DeleteTaskRequest req, CancellationToken ct)
    {
        var ok = await _mediator.Send(new DeleteTaskCommand(req.Id), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

