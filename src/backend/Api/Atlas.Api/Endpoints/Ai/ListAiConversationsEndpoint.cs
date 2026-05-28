using Atlas.Api.DTOs.Ai;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Ai.ListConversations;
using Atlas.Domain.Entities;

namespace Atlas.Api.Endpoints.Ai;

public sealed class ListAiConversationsEndpoint : Endpoint<ListAiConversationsRequest, IReadOnlyList<AiConversationListItemDto>>
{
    private readonly IMediator _mediator;

    public ListAiConversationsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/ai/conversations");
        AllowAnonymous();
        Summary(s => { s.Summary = "List recent AI conversations"; });
    }

    public override async Task HandleAsync(ListAiConversationsRequest req, CancellationToken ct)
    {
        IReadOnlyList<AiConversation> conversations = await _mediator.Send(new ListAiConversationsQuery(req.Take ?? 25), ct);
        IReadOnlyList<AiConversationListItemDto> dtos = conversations.Select(AiConversationMapper.ToListItemDto).ToList();
        await Send.OkAsync(dtos, ct);
    }
}
