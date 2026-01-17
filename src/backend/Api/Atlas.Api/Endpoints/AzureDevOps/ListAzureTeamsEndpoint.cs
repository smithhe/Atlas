using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Teams;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class ListAzureTeamsEndpoint : Endpoint<AzureTeamsRequest, IReadOnlyList<AzureTeamDto>>
{
    private readonly IMediator _mediator;

    public ListAzureTeamsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/azure-devops/teams");
        AllowAnonymous();
        Summary(s => { s.Summary = "List Azure DevOps teams for a project"; });
    }

    public override async Task HandleAsync(AzureTeamsRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Organization) || string.IsNullOrWhiteSpace(req.ProjectId))
        {
            AddError("organization", "Organization is required.");
            AddError("projectId", "ProjectId is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var teams = await _mediator.Send(new ListAzureTeamsQuery(req.Organization, req.ProjectId), ct);
        var dto = teams.Select(t => new AzureTeamDto(t.Id, t.Name)).ToList();
        await Send.OkAsync(dto, ct);
    }
}
