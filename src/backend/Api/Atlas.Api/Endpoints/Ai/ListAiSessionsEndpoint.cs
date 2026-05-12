using Atlas.Api.DTOs.Ai;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Ai.ListSessions;
using Atlas.Domain.Entities;

namespace Atlas.Api.Endpoints.Ai;

public sealed class ListAiSessionsEndpoint : Endpoint<ListAiSessionsRequest, IReadOnlyList<AiSessionListItemDto>>
{
    private readonly IMediator _mediator;

    public ListAiSessionsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/ai/sessions");
        AllowAnonymous();
        Summary(s => { s.Summary = "List recent AI sessions"; });
    }

    public override async Task HandleAsync(ListAiSessionsRequest req, CancellationToken ct)
    {
        IReadOnlyList<AiSession> sessions = await _mediator.Send(new ListAiSessionsQuery(req.Take ?? 25), ct);
        IReadOnlyList<AiSessionListItemDto> dtos = sessions.Select(AiMapper.ToListItemDto).ToList();
        await Send.OkAsync(dtos, ct);
    }
}
