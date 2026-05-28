using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class AiConversationRepository : IAiConversationRepository
{
    private readonly AtlasDbContext _db;

    public AiConversationRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        await _db.AiConversations.AddAsync(conversation, cancellationToken);
    }

    public Task<AiConversation?> GetByIdWithTurnsAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return _db.AiConversations
            .Include(x => x.Turns.OrderBy(t => t.TurnIndex))
            .ThenInclude(t => t.Events.OrderBy(e => e.Sequence))
            .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);
    }

    public async Task<IReadOnlyList<AiConversation>> ListRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        int safeTake = Math.Clamp(take, 1, 100);
        return await _db.AiConversations
            .Include(x => x.Turns)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(safeTake)
            .ToListAsync(cancellationToken);
    }

    public async Task TouchUpdatedAtAsync(Guid conversationId, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default)
    {
        AiConversation? conversation = await _db.AiConversations.FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);
        if (conversation is null)
        {
            return;
        }

        conversation.UpdatedAtUtc = updatedAtUtc;
    }
}
