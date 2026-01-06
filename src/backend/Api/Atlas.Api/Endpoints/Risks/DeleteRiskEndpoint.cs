using Atlas.Api.DTOs.Risks;
using Atlas.Application.Features.Risks.DeleteRisk;

namespace Atlas.Api.Endpoints.Risks;

public sealed class DeleteRiskEndpoint : Endpoint<DeleteRiskRequest>
{
    private readonly IMediator _mediator;

    public DeleteRiskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/risks/{id:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Delete a risk"; });
    }

    public override async Task HandleAsync(DeleteRiskRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        req = req with { Id = id };

        var ok = await _mediator.Send(new DeleteRiskCommand(req.Id), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

