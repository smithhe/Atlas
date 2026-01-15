using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Users;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class ListAzureUsersEndpoint : Endpoint<AzureOrganizationRequest, IReadOnlyList<AzureUserDto>>
{
    private readonly IMediator _mediator;

    public ListAzureUsersEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/azure-devops/users");
        AllowAnonymous();
        Summary(s => { s.Summary = "List Azure DevOps users for an organization"; });
    }

    public override async Task HandleAsync(AzureOrganizationRequest req, CancellationToken ct)
    {
        var users = await _mediator.Send(new ListAzureUsersQuery(req.Organization), ct);
        var dto = users.Select(u => new AzureUserDto(u.DisplayName, u.UniqueName, u.Descriptor)).ToList();
        await Send.OkAsync(dto, ct);
    }
}
