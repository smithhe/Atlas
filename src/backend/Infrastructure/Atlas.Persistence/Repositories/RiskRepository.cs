using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class RiskRepository : IRiskRepository
{
    private readonly AtlasDbContext _db;

    public RiskRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<Risk?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Risks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Risk?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Risks
            .Include(x => x.Project)
            .Include(x => x.Tasks)
            .Include(x => x.LinkedTeamMembers)
            .Include(x => x.History)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Risk>> ListAsync(CancellationToken cancellationToken = default)
    {
        // SQLite doesn't support ordering by DateTimeOffset; order client-side for now.
        var risks = await _db.Risks.ToListAsync(cancellationToken);
        return risks
            .OrderByDescending(x => x.LastUpdatedAt)
            .ToList();
    }

    public async Task AddAsync(Risk risk, CancellationToken cancellationToken = default)
    {
        await _db.Risks.AddAsync(risk, cancellationToken);
    }

    public void Remove(Risk risk)
    {
        _db.Risks.Remove(risk);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

