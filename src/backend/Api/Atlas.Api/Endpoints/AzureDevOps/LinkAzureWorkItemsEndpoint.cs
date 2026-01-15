using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Import;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class LinkAzureWorkItemsEndpoint : Endpoint<LinkAzureWorkItemsRequest, int>
{
    private readonly IMediator _mediator;

    public LinkAzureWorkItemsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/azure-devops/import/link");
        AllowAnonymous();
        Summary(s => { s.Summary = "Bulk link Azure work items to a local project"; });
    }

    public override async Task HandleAsync(LinkAzureWorkItemsRequest req, CancellationToken ct)
    {
        var updated = await _mediator.Send(
            new LinkAzureWorkItemsCommand(req.AzureWorkItemIds, req.ProjectId, req.TeamMemberId),
            ct);
        await Send.OkAsync(updated, ct);
    }
}
