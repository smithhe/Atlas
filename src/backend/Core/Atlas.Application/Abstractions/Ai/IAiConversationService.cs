namespace Atlas.Application.Abstractions.Ai;

public interface IAiConversationService
{
    Task<(Guid ConversationId, Guid TurnSessionId)> StartConversationAsync(AiSessionStartRequest request, CancellationToken cancellationToken);

    Task<Guid> ContinueConversationAsync(Guid conversationId, string prompt, CancellationToken cancellationToken);
}
