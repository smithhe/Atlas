using Atlas.Api.DTOs.Risks;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Risks.ListRisks;

namespace Atlas.Api.Endpoints.Risks;

public sealed class ListRisksEndpoint : Endpoint<ListRisksRequest, IReadOnlyList<RiskListItemDto>>
{
    private readonly IMediator _mediator;

    public ListRisksEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/risks");
        AllowAnonymous();
        Summary(s => { s.Summary = "List risks"; });
    }

    public override async Task HandleAsync(ListRisksRequest req, CancellationToken ct)
    {
        var risks = await _mediator.Send(new ListRisksQuery(), ct);
        var dtos = risks.Select(RiskMapper.ToListItemDto).ToList();
        await Send.OkAsync(dtos, ct);
    }
}

