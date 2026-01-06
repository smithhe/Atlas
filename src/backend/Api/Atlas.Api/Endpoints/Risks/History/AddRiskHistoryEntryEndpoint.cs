using Atlas.Api.DTOs.Risks.History;
using Atlas.Application.Features.Risks.History.AddRiskHistoryEntry;

namespace Atlas.Api.Endpoints.Risks.History;

public sealed class AddRiskHistoryEntryEndpoint : Endpoint<AddRiskHistoryEntryRequest, AddRiskHistoryEntryResponse>
{
    private readonly IMediator _mediator;

    public AddRiskHistoryEntryEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/risks/{riskId:guid}/history");
        AllowAnonymous();
        Summary(s => { s.Summary = "Add a history entry to a risk"; });
    }

    public override async Task HandleAsync(AddRiskHistoryEntryRequest req, CancellationToken ct)
    {
        var riskId = Route<Guid>("riskId");
        req = req with { RiskId = riskId };

        var id = await _mediator.Send(new AddRiskHistoryEntryCommand(req.RiskId, req.Text), ct);
        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new AddRiskHistoryEntryResponse(id), 201, ct);
    }
}

