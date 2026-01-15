using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.RunAzureSync;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class RunAzureSyncEndpoint : EndpointWithoutRequest<AzureSyncResultDto>
{
    private readonly IMediator _mediator;

    public RunAzureSyncEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/azure-devops/sync");
        AllowAnonymous();
        Summary(s => { s.Summary = "Run Azure DevOps sync now"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _mediator.Send(new RunAzureSyncCommand(), ct);

        await Send.OkAsync(new AzureSyncResultDto(
            result.Succeeded,
            result.ItemsFetched,
            result.ItemsUpserted,
            result.LastChangedUtc,
            result.LastWorkItemId,
            result.Error), ct);
    }
}
