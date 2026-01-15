using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.SyncState;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class GetAzureSyncStateEndpoint : EndpointWithoutRequest<AzureSyncStateDto>
{
    private readonly IMediator _mediator;

    public GetAzureSyncStateEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/azure-devops/sync-state");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get Azure DevOps sync state"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var state = await _mediator.Send(new GetAzureSyncStateQuery(), ct);
        if (state is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new AzureSyncStateDto(
            state.LastSuccessfulChangedUtc,
            state.LastSuccessfulWorkItemId,
            state.LastAttemptedAtUtc,
            state.LastCompletedAtUtc,
            state.LastRunStatus.ToString(),
            state.LastError), ct);
    }
}
