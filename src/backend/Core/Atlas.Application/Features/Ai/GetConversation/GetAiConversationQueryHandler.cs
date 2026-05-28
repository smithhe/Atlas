using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.GetConversation;

public sealed class GetAiConversationQueryHandler : IRequestHandler<GetAiConversationQuery, AiConversation?>
{
    private readonly IAiConversationRepository _conversations;

    public GetAiConversationQueryHandler(IAiConversationRepository conversations)
    {
        _conversations = conversations;
    }

    public Task<AiConversation?> Handle(GetAiConversationQuery request, CancellationToken cancellationToken)
    {
        return _conversations.GetByIdWithTurnsAsync(request.ConversationId, cancellationToken);
    }
}
