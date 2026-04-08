using Atlas.Api.DTOs.Projects;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Projects.GetProject;
using Atlas.Domain.Entities;

namespace Atlas.Api.Endpoints.Projects;

public sealed class GetProjectEndpoint : Endpoint<GetProjectRequest, ProjectDto>
{
    private readonly IMediator _mediator;

    public GetProjectEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/projects/{id:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get a project by id"; });
    }

    public override async Task HandleAsync(GetProjectRequest req, CancellationToken ct)
    {
        Guid id = Route<Guid>("id");
        req = new GetProjectRequest(Id: id);

        Project? project = await _mediator.Send(new GetProjectByIdQuery(req.Id, IncludeDetails: true), ct);
        if (project is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(ProjectMapper.ToDto(project), ct);
    }
}

