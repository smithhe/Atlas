using Atlas.Api.DTOs.Growth;
using Atlas.Application.Features.Growth.UpdateFocusAreas;

namespace Atlas.Api.Endpoints.Growth;

public sealed class UpdateGrowthFocusAreasEndpoint : Endpoint<UpdateGrowthFocusAreasRequest>
{
    private readonly IMediator _mediator;

    public UpdateGrowthFocusAreasEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/growth/{growthId:guid}/focus-areas");
        AllowAnonymous();
        Summary(s => { s.Summary = "Update growth plan focus areas markdown"; });
    }

    public override async Task HandleAsync(UpdateGrowthFocusAreasRequest req, CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        req = req with { GrowthId = growthId };

        var ok = await _mediator.Send(new UpdateGrowthFocusAreasCommand(req.GrowthId, req.FocusAreasMarkdown), ct);
        if (!ok)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

