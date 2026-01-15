using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Connection;

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
        var connection = await _mediator.Send(new GetAzureConnectionQuery(), ct);
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
            connection.IsEnabled);

        await Send.OkAsync(dto, ct);
    }
}
