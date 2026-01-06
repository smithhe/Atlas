using Atlas.Api.DTOs.Projects;
using Atlas.Application.Features.Projects.DeleteProject;

namespace Atlas.Api.Endpoints.Projects;

public sealed class DeleteProjectEndpoint : Endpoint<DeleteProjectRequest>
{
    private readonly IMediator _mediator;

    public DeleteProjectEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/projects/{id:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete a project"; });
    }

    public override async Task HandleAsync(DeleteProjectRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        req = req with { Id = id };

        var ok = await _mediator.Send(new DeleteProjectCommand(req.Id), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

