using Atlas.Api.DTOs.Ai;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Ai.GetSession;
using Atlas.Domain.Entities;

namespace Atlas.Api.Endpoints.Ai;

public sealed class GetAiSessionEndpoint : Endpoint<GetAiSessionRequest, AiSessionDetailDto>
{
    private readonly IMediator _mediator;

    public GetAiSessionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/ai/sessions/{sessionId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get an AI session with event history"; });
    }

    public override async Task HandleAsync(GetAiSessionRequest req, CancellationToken ct)
    {
        AiSession? session = await _mediator.Send(new GetAiSessionQuery(req.SessionId), ct);
        if (session is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(AiMapper.ToDetailDto(session), ct);
    }
}
