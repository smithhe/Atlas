using Atlas.Api.DTOs.Risks;
using Atlas.Application.Features.Risks.CreateRisk;

namespace Atlas.Api.Endpoints.Risks;

public sealed class CreateRiskEndpoint : Endpoint<CreateRiskRequest, CreateRiskResponse>
{
    private readonly IMediator _mediator;

    public CreateRiskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/risks");
        AllowAnonymous();
        Summary(s => { s.Summary = "Create a risk"; });
    }

    public override async Task HandleAsync(CreateRiskRequest req, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateRiskCommand(
            req.Title,
            req.Status,
            req.Severity,
            req.ProjectId,
            req.Description,
            req.Evidence), ct);

        if (id == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new CreateRiskResponse(id), 201, ct);
    }
}

