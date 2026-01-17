using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Users;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class ListAzureUsersEndpoint : Endpoint<AzureTeamMembersRequest, IReadOnlyList<AzureUserDto>>
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

    public override async Task HandleAsync(AzureTeamMembersRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Organization) || string.IsNullOrWhiteSpace(req.ProjectId) || string.IsNullOrWhiteSpace(req.TeamId))
        {
            AddError("organization", "Organization is required.");
            AddError("projectId", "ProjectId is required.");
            AddError("teamId", "TeamId is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var users = await _mediator.Send(new ListAzureUsersQuery(req.Organization, req.ProjectId, req.TeamId), ct);
        var dto = users.Select(u => new AzureUserDto(u.DisplayName, u.UniqueName, u.Descriptor)).ToList();
        await Send.OkAsync(dto, ct);
    }
}
