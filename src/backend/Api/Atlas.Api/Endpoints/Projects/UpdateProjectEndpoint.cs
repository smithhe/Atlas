using Atlas.Api.DTOs.Projects;
using Atlas.Application.Features.Projects.UpdateProject;
using ProjectLinkDto = Atlas.Application.DTOs.ProjectLinkDto;

namespace Atlas.Api.Endpoints.Projects;

public sealed class UpdateProjectEndpoint : Endpoint<UpdateProjectRequest>
{
    private readonly IMediator _mediator;

    public UpdateProjectEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/projects/{id:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update a project"; });
    }

    public override async Task HandleAsync(UpdateProjectRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var links = req.Links?.Select(l => new ProjectLinkDto(l.Label, l.Url)).ToList();

        var ok = await _mediator.Send(new UpdateProjectCommand(
            id,
            req.Name,
            req.Summary,
            req.Description,
            req.Status,
            req.Health,
            req.TargetDate,
            req.Priority,
            req.ProductOwnerId,
            req.Tags,
            links), ct);

        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

