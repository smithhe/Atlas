using Atlas.Api.DTOs.Growth;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Growth.GetGrowth;

namespace Atlas.Api.Endpoints.Growth;

public sealed class GetGrowthEndpoint : EndpointWithoutRequest<GrowthDto>
{
    private readonly IMediator _mediator;

    public GetGrowthEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/growth/{growthId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get a growth plan by id"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var growthId = Route<Guid>("growthId");
        var growth = await _mediator.Send(new GetGrowthByIdQuery(growthId), ct);
        if (growth is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(GrowthMapper.ToDto(growth), ct);
    }
}

