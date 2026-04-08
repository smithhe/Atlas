using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Connection;
using Atlas.Domain.Entities;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class GetAzureConnectionEndpoint : EndpointWithoutRequest<AzureConnectionDto>
{
    private readonly IMediator _mediator;

    public GetAzureConnectionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/azure-devops/connection");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get Azure DevOps connection settings (singleton)"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        AzureConnection? connection = await _mediator.Send(new GetAzureConnectionQuery(), ct);
        if (connection is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var dto = new AzureConnectionDto(
            connection.Organization,
            connection.Project,
            connection.AreaPath,
            connection.TeamName,
            connection.IsEnabled,
            connection.ProjectId,
            connection.TeamId);

        await Send.OkAsync(dto, ct);
    }
}
