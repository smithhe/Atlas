using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Connection;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class UpdateAzureConnectionEndpoint : Endpoint<UpdateAzureConnectionRequest>
{
    private readonly IMediator _mediator;

    public UpdateAzureConnectionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/azure-devops/connection");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update Azure DevOps connection settings (singleton)"; });
    }

    public override async Task HandleAsync(UpdateAzureConnectionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ProjectId) || string.IsNullOrWhiteSpace(req.TeamId))
        {
            AddError("projectId", "ProjectId is required.");
            AddError("teamId", "TeamId is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await _mediator.Send(
            new UpdateAzureConnectionCommand(
                req.Organization,
                req.Project,
                req.AreaPath,
                req.TeamName,
                req.IsEnabled,
                req.ProjectId,
                req.TeamId),
            ct);
        await Send.NoContentAsync(ct);
    }
}
