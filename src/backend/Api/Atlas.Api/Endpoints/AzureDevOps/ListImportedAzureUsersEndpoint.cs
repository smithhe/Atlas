using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Users;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class ListImportedAzureUsersEndpoint : EndpointWithoutRequest<IReadOnlyList<AzureUserDto>>
{
    private readonly IMediator _mediator;

    public ListImportedAzureUsersEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/azure-devops/import/users");
        AllowAnonymous();
        Summary(s => { s.Summary = "List imported Azure DevOps users"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var users = await _mediator.Send(new ListImportedAzureUsersQuery(), ct);
        var dto = users.Select(u => new AzureUserDto(u.DisplayName, u.UniqueName, u.Descriptor)).ToList();
        await Send.OkAsync(dto, ct);
    }
}
