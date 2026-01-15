using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class AzureWorkItemRepository : IAzureWorkItemRepository
{
    private readonly AtlasDbContext _db;

    public AzureWorkItemRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AzureWorkItem>> GetByWorkItemIdsAsync(Guid connectionId, IReadOnlyList<int> workItemIds, CancellationToken cancellationToken = default)
    {
        if (workItemIds.Count == 0) return [];

        return await _db.AzureWorkItems
            .Where(x => x.AzureConnectionId == connectionId && workItemIds.Contains(x.WorkItemId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AzureWorkItem>> ListUnlinkedAsync(Guid connectionId, int take, CancellationToken cancellationToken = default)
    {
        return await _db.AzureWorkItems
            .AsNoTracking()
            .Where(x => x.AzureConnectionId == connectionId)
            .Where(x => !_db.AzureWorkItemLinks.Any(l => l.AzureWorkItemId == x.Id))
            .OrderByDescending(x => x.ChangedDateUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AzureWorkItem workItem, CancellationToken cancellationToken = default)
    {
        await _db.AzureWorkItems.AddAsync(workItem, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
