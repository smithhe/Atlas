using Atlas.Api.DTOs.Projects;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Projects.ListProjects;

namespace Atlas.Api.Endpoints.Projects;

public sealed class ListProjectsEndpoint : Endpoint<ListProjectsRequest, IReadOnlyList<ProjectListItemDto>>
{
    private readonly IMediator _mediator;

    public ListProjectsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/projects");
        AllowAnonymous();
        Summary(s => { s.Summary = "List projects"; });
    }

    public override async Task HandleAsync(ListProjectsRequest req, CancellationToken ct)
    {
        var projects = await _mediator.Send(new ListProjectsQuery(), ct);
        var dtos = projects.Select(ProjectMapper.ToListItemDto).ToList();
        await Send.OkAsync(dtos, ct);
    }
}

