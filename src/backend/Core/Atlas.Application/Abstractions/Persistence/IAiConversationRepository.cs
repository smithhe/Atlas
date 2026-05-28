using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAiConversationRepository
{
    Task AddAsync(AiConversation conversation, CancellationToken cancellationToken = default);
    Task<AiConversation?> GetByIdWithTurnsAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiConversation>> ListRecentAsync(int take, CancellationToken cancellationToken = default);
    Task TouchUpdatedAtAsync(Guid conversationId, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default);
}
