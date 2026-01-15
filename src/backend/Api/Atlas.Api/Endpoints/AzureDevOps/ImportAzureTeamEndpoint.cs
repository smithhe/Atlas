using Atlas.Api.DTOs.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Team;

namespace Atlas.Api.Endpoints.AzureDevOps;

public sealed class ImportAzureTeamEndpoint : Endpoint<ImportAzureTeamRequest, ImportAzureTeamResultDto>
{
    private readonly IMediator _mediator;

    public ImportAzureTeamEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/azure-devops/team/import");
        AllowAnonymous();
        Summary(s => { s.Summary = "Import selected Azure users into local team members"; });
    }

    public override async Task HandleAsync(ImportAzureTeamRequest req, CancellationToken ct)
    {
        var selections = req.Users.Select(u => new AzureTeamMemberSelection(u.DisplayName, u.UniqueName, u.Descriptor)).ToList();
        var result = await _mediator.Send(new ImportAzureTeamMembersCommand(selections), ct);

        await Send.OkAsync(new ImportAzureTeamResultDto(
            result.UsersAdded,
            result.UsersUpdated,
            result.TeamMembersCreated,
            result.MappingsCreated), ct);
    }
}
