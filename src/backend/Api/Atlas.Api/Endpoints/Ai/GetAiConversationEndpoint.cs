using Atlas.Api.DTOs.Ai;
using Atlas.Api.Mappers;
using Atlas.Application.Features.Ai.GetConversation;
using Atlas.Domain.Entities;

namespace Atlas.Api.Endpoints.Ai;

public sealed class GetAiConversationEndpoint : Endpoint<GetAiConversationRequest, AiConversationDetailDto>
{
    private readonly IMediator _mediator;

    public GetAiConversationEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/ai/conversations/{conversationId:guid}");
        AllowAnonymous();
        Summary(s => { s.Summary = "Get an AI conversation with all turns"; });
    }

    public override async Task HandleAsync(GetAiConversationRequest req, CancellationToken ct)
    {
        AiConversation? conversation = await _mediator.Send(new GetAiConversationQuery(req.ConversationId), ct);
        if (conversation is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(AiConversationMapper.ToDetailDto(conversation), ct);
    }
}
