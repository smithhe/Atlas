using Atlas.Api.DTOs.Risks;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Risks.GetRisk;
using Atlas.Domain.Entities;

namespace Atlas.Api.Endpoints.Risks;

public sealed class GetRiskEndpoint : Endpoint<GetRiskRequest, RiskDto>
{
    private readonly IMediator _mediator;

    public GetRiskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/risks/{id:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get a risk by id"; });
    }

    public override async Task HandleAsync(GetRiskRequest req, CancellationToken ct)
    {
        Guid id = Route<Guid>("id");
        req = new GetRiskRequest(Id: id);

        Risk? risk = await _mediator.Send(new GetRiskByIdQuery(req.Id, IncludeDetails: true), ct);
        if (risk is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(RiskMapper.ToDto(risk), ct);
    }
}

