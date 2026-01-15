using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Import;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class ListAzureImportWorkItemsEndpoint : EndpointWithoutRequest<IReadOnlyList<AzureImportWorkItemDto>>
{
    private readonly IMediator _mediator;

    public ListAzureImportWorkItemsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/azure-devops/import/work-items");
        AllowAnonymous();
        Summary(s => { s.Summary = "List Azure work items awaiting import/linking"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await _mediator.Send(new GetAzureImportWorkItemsQuery(), ct);
        var dto = items.Select(x => new AzureImportWorkItemDto(
            x.Id,
            x.WorkItemId,
            x.Title,
            x.State,
            x.WorkItemType,
            x.AreaPath,
            x.IterationPath,
            x.ChangedDateUtc,
            x.AssignedToUniqueName,
            x.Url,
            x.SuggestedTeamMemberId)).ToList();

        await Send.OkAsync(dto, ct);
    }
}
