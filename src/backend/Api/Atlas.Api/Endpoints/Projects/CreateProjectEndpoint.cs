using Atlas.Application.DTOs;
using Atlas.Application.Features.Projects.CreateProject;
using ProjectLinkDto = Atlas.Application.DTOs.ProjectLinkDto;
using Atlas.Api.DTOs.Projects;

namespace Atlas.Api.Endpoints.Projects;

public sealed class CreateProjectEndpoint : Endpoint<CreateProjectRequest, CreateProjectResponse>
{
    private readonly IMediator _mediator;

    public CreateProjectEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/projects");
        AllowAnonymous();
        Summary(s => { s.Summary = "Create a project"; });
    }

    public override async Task HandleAsync(CreateProjectRequest req, CancellationToken ct)
    {
        var links = req.Links?.Select(l => new ProjectLinkDto(l.Label, l.Url)).ToList();

        var id = await _mediator.Send(new CreateProjectCommand(
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

        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new CreateProjectResponse(id), 201, ct);
    }
}

