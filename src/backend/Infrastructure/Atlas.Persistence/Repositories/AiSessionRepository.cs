using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class AiSessionRepository : IAiSessionRepository
{
    private readonly AtlasDbContext _db;

    public AiSessionRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AiSession session, CancellationToken cancellationToken = default)
    {
        await _db.AiSessions.AddAsync(session, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return _db.AiSessions.AnyAsync(x => x.Id == sessionId, cancellationToken);
    }

    public Task<AiSession?> GetByIdWithEventsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return _db.AiSessions
            .Include(x => x.Events.OrderBy(e => e.Sequence))
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<AiSession>> ListRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        int safeTake = Math.Clamp(take, 1, 100);
        return await _db.AiSessions
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(safeTake)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiSessionEvent>> ListEventsAsync(Guid sessionId, int? afterSequence = null, CancellationToken cancellationToken = default)
    {
        IQueryable<AiSessionEvent> query = _db.AiSessionEvents
            .Where(x => x.AiSessionId == sessionId);

        if (afterSequence.HasValue)
        {
            query = query.Where(x => x.Sequence > afterSequence.Value);
        }

        return await query
            .OrderBy(x => x.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task<AiSessionEvent> AppendEventAsync(Guid sessionId, AiSessionEvent evt, CancellationToken cancellationToken = default)
    {
        AiSession session = await _db.AiSessions.FirstAsync(x => x.Id == sessionId, cancellationToken);
        int lastSequence = await _db.AiSessionEvents
            .Where(x => x.AiSessionId == sessionId)
            .Select(x => (int?)x.Sequence)
            .MaxAsync(cancellationToken) ?? 0;

        evt.Id = evt.Id == Guid.Empty ? Guid.NewGuid() : evt.Id;
        evt.AiSessionId = sessionId;
        evt.Sequence = lastSequence + 1;

        session.Status = evt.Status ?? session.Status;
        session.IsTerminal = session.IsTerminal || evt.IsTerminal;
        if (evt.IsTerminal)
        {
            session.CompletedAtUtc = evt.OccurredAtUtc;
        }

        await _db.AiSessionEvents.AddAsync(evt, cancellationToken);
        return evt;
    }
}
