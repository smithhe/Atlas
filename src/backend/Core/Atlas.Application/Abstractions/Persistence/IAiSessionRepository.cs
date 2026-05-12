using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAiSessionRepository
{
    Task AddAsync(AiSession session, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<AiSession?> GetByIdWithEventsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiSession>> ListRecentAsync(int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiSessionEvent>> ListEventsAsync(Guid sessionId, int? afterSequence = null, CancellationToken cancellationToken = default);
    Task<AiSessionEvent> AppendEventAsync(Guid sessionId, AiSessionEvent evt, CancellationToken cancellationToken = default);
}
