using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class GrowthRepository : IGrowthRepository
{
    private readonly AtlasDbContext _db;

    public GrowthRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<Growth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.GrowthPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Growth?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.GrowthPlans
            .Include(x => x.SkillsInProgress)
            .Include(x => x.FeedbackThemes)
            .Include(x => x.Goals)
            .ThenInclude(x => x.Actions)
            .Include(x => x.Goals)
            .ThenInclude(x => x.CheckIns)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Growth?> GetByTeamMemberIdAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        return _db.GrowthPlans.FirstOrDefaultAsync(x => x.TeamMemberId == teamMemberId, cancellationToken);
    }

    public Task<Growth?> GetByTeamMemberIdWithDetailsAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        return _db.GrowthPlans
            .Where(x => x.TeamMemberId == teamMemberId)
            .Include(x => x.SkillsInProgress)
            .Include(x => x.FeedbackThemes)
            .Include(x => x.Goals)
            .ThenInclude(x => x.Actions)
            .Include(x => x.Goals)
            .ThenInclude(x => x.CheckIns)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Growth growth, CancellationToken cancellationToken = default)
    {
        await _db.GrowthPlans.AddAsync(growth, cancellationToken);
    }

    public void Remove(Growth growth)
    {
        _db.GrowthPlans.Remove(growth);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

