using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Areas;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class ListAzureTeamAreaPathsEndpoint : Endpoint<AzureTeamAreaPathsRequest, AzureTeamAreaPathsDto>
{
    private readonly IMediator _mediator;

    public ListAzureTeamAreaPathsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/azure-devops/team-area-paths");
        AllowAnonymous();
        Summary(s => { s.Summary = "List Azure DevOps team area paths (team settings)"; });
    }

    public override async Task HandleAsync(AzureTeamAreaPathsRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Organization) ||
            string.IsNullOrWhiteSpace(req.ProjectId) ||
            string.IsNullOrWhiteSpace(req.TeamName))
        {
            AddError("organization", "Organization is required.");
            AddError("projectId", "ProjectId is required.");
            AddError("teamName", "TeamName is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var result = await _mediator.Send(new ListAzureTeamAreaPathsQuery(req.Organization, req.ProjectId, req.TeamName), ct);
        var dto = new AzureTeamAreaPathsDto(
            result.DefaultValue,
            result.Values.Select(v => new AzureTeamAreaPathDto(v.Value, v.IncludeChildren)).ToList());

        await Send.OkAsync(dto, ct);
    }
}

