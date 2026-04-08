using Atlas.Api.DTOs.Risks.History;
using Atlas.Application.Features.Risks.History.DeleteRiskHistoryEntry;

namespace Atlas.Api.Endpoints.Risks.History;

public sealed class DeleteRiskHistoryEntryEndpoint : Endpoint<DeleteRiskHistoryEntryRequest>
{
    private readonly IMediator _mediator;

    public DeleteRiskHistoryEntryEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/risks/{riskId:guid}/history/{entryId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete a risk history entry"; });
    }

    public override async Task HandleAsync(DeleteRiskHistoryEntryRequest req, CancellationToken ct)
    {
        Guid riskId = Route<Guid>("riskId");
        Guid entryId = Route<Guid>("entryId");
        req = new DeleteRiskHistoryEntryRequest(RiskId: riskId, EntryId: entryId);

        var ok = await _mediator.Send(new DeleteRiskHistoryEntryCommand(req.RiskId, req.EntryId), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

