using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAiSessionRepository
{
    Task AddAsync(AiSession session, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<AiSession?> GetByIdWithEventsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiSession>> ListByConversationIdWithEventsAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<AiSession?> GetLatestTurnAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<int> GetNextTurnIndexAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiSessionEvent>> ListEventsAsync(Guid sessionId, int? afterSequence = null, CancellationToken cancellationToken = default);
    Task<AiSessionEvent> AppendEventAsync(Guid sessionId, AiSessionEvent evt, CancellationToken cancellationToken = default);
}
