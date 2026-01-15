using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Projects;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class ListAzureProjectsEndpoint : Endpoint<AzureOrganizationRequest, IReadOnlyList<AzureProjectDto>>
{
    private readonly IMediator _mediator;

    public ListAzureProjectsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/azure-devops/projects");
        AllowAnonymous();
        Summary(s => { s.Summary = "List Azure DevOps projects for an organization"; });
    }

    public override async Task HandleAsync(AzureOrganizationRequest req, CancellationToken ct)
    {
        var projects = await _mediator.Send(new ListAzureProjectsQuery(req.Organization), ct);
        var dto = projects.Select(p => new AzureProjectDto(p.Id, p.Name)).ToList();
        await Send.OkAsync(dto, ct);
    }
}
