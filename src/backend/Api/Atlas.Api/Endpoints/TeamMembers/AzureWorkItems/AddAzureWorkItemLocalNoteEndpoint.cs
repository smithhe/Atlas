using Atlas.Api.DTOs.TeamMembers.AzureWorkItems;
using Atlas.Application.Features.TeamMembers.AzureWorkItems.AddAzureWorkItemLocalNote;

namespace Atlas.Api.Endpoints.TeamMembers.AzureWorkItems;

public sealed class AddAzureWorkItemLocalNoteEndpoint : Endpoint<AddAzureWorkItemLocalNoteRequest, AddAzureWorkItemLocalNoteResponse>
{
    private readonly IMediator _mediator;

    public AddAzureWorkItemLocalNoteEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/team-members/{teamMemberId:guid}/azure-work-items/{workItemId:int}/notes");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Add a local note to a team member Azure work item";
            s.Response<AddAzureWorkItemLocalNoteResponse>(201, "Created");
        });
    }

    public override async Task HandleAsync(AddAzureWorkItemLocalNoteRequest req, CancellationToken ct)
    {
        Guid teamMemberId = Route<Guid>("teamMemberId");
        int workItemId = Route<int>("workItemId");
        req = req with { TeamMemberId = teamMemberId, WorkItemId = workItemId };

        Guid id = await _mediator.Send(new AddAzureWorkItemLocalNoteCommand(req.TeamMemberId, req.WorkItemId, req.Text), ct);
        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new AddAzureWorkItemLocalNoteResponse(id, DateTimeOffset.UtcNow), 201, ct);
    }
}
