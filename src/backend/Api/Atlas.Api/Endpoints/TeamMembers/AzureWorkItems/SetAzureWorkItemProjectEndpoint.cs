using Atlas.Api.DTOs.TeamMembers.AzureWorkItems;
using Atlas.Application.Features.TeamMembers.AzureWorkItems.SetAzureWorkItemProject;

namespace Atlas.Api.Endpoints.TeamMembers.AzureWorkItems;

public sealed class SetAzureWorkItemProjectEndpoint : Endpoint<SetAzureWorkItemProjectRequest>
{
    private readonly IMediator _mediator;

    public SetAzureWorkItemProjectEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/team-members/{teamMemberId:guid}/azure-work-items/{workItemId:int}/project");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Assign or clear the Atlas project linked to a team member Azure work item";
            s.Response(204, "No Content");
            s.Response(400, "Validation error");
            s.Response(404, "Not Found");
        });
    }

    public override async Task HandleAsync(SetAzureWorkItemProjectRequest req, CancellationToken ct)
    {
        Guid teamMemberId = Route<Guid>("teamMemberId");
        int workItemId = Route<int>("workItemId");
        req = req with { TeamMemberId = teamMemberId, WorkItemId = workItemId };

        bool ok = await _mediator.Send(new SetAzureWorkItemProjectCommand(req.TeamMemberId, req.WorkItemId, req.ProjectId), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
