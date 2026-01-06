using Atlas.Api.DTOs.Risks;
using Atlas.Application.Features.Risks.UpdateRisk;

namespace Atlas.Api.Endpoints.Risks;

public sealed class UpdateRiskEndpoint : Endpoint<UpdateRiskRequest>
{
    private readonly IMediator _mediator;

    public UpdateRiskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/risks/{id:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update a risk"; });
    }

    public override async Task HandleAsync(UpdateRiskRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var ok = await _mediator.Send(new UpdateRiskCommand(
            id,
            req.Title,
            req.Status,
            req.Severity,
            req.ProjectId,
            req.Description,
            req.Evidence), ct);

        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

