using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.ListConversations;

public sealed class ListAiConversationsQueryHandler : IRequestHandler<ListAiConversationsQuery, IReadOnlyList<AiConversation>>
{
    private readonly IAiConversationRepository _conversations;

    public ListAiConversationsQueryHandler(IAiConversationRepository conversations)
    {
        _conversations = conversations;
    }

    public Task<IReadOnlyList<AiConversation>> Handle(ListAiConversationsQuery request, CancellationToken cancellationToken)
    {
        return _conversations.ListRecentAsync(request.Take, cancellationToken);
    }
}
