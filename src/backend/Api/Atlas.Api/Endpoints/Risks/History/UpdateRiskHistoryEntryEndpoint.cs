using Atlas.Api.DTOs.Risks.History;
using Atlas.Application.Features.Risks.History.UpdateRiskHistoryEntry;

namespace Atlas.Api.Endpoints.Risks.History;

public sealed class UpdateRiskHistoryEntryEndpoint : Endpoint<UpdateRiskHistoryEntryRequest>
{
    private readonly IMediator _mediator;

    public UpdateRiskHistoryEntryEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/risks/{riskId:guid}/history/{entryId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update a risk history entry"; });
    }

    public override async Task HandleAsync(UpdateRiskHistoryEntryRequest req, CancellationToken ct)
    {
        var riskId = Route<Guid>("riskId");
        var entryId = Route<Guid>("entryId");
        req = req with { RiskId = riskId, EntryId = entryId };

        var ok = await _mediator.Send(new UpdateRiskHistoryEntryCommand(req.RiskId, req.EntryId, req.Text), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

