namespace Atlas.Application.Abstractions.Ai;

public interface IAiSessionStore
{
    Task CreateSessionAsync(Guid sessionId, AiSessionStartRequest request, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid sessionId, CancellationToken cancellationToken);
    ValueTask PublishEventAsync(AiSessionEvent evt, CancellationToken cancellationToken);
    IAsyncEnumerable<AiSessionEvent> StreamEventsAsync(Guid sessionId, CancellationToken cancellationToken);
}

