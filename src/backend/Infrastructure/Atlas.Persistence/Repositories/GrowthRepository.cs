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

    public async Task<Growth?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _db.GrowthPlans
            .Include(x => x.SkillsInProgress)
            .Include(x => x.FeedbackThemes)
            .Include(x => x.Goals)
            .ThenInclude(x => x.Actions)
            .Include(x => x.Goals)
            .ThenInclude(x => x.CheckIns)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        NormalizeSkillOrder(plan);
        return plan;
    }

    public Task<Growth?> GetByTeamMemberIdAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        return _db.GrowthPlans.FirstOrDefaultAsync(x => x.TeamMemberId == teamMemberId, cancellationToken);
    }

    public async Task<Growth?> GetByTeamMemberIdWithDetailsAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        var plan = await _db.GrowthPlans
            .Where(x => x.TeamMemberId == teamMemberId)
            .Include(x => x.SkillsInProgress)
            .Include(x => x.FeedbackThemes)
            .Include(x => x.Goals)
            .ThenInclude(x => x.Actions)
            .Include(x => x.Goals)
            .ThenInclude(x => x.CheckIns)
            .FirstOrDefaultAsync(cancellationToken);

        NormalizeSkillOrder(plan);
        return plan;
    }

    private static void NormalizeSkillOrder(Growth? plan)
    {
        if (plan?.SkillsInProgress is null || plan.SkillsInProgress.Count == 0) return;
        
        plan.SkillsInProgress = plan.SkillsInProgress
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Value, StringComparer.Ordinal)
            .ToList();
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

