using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class AzureWorkItemLinkRepository : IAzureWorkItemLinkRepository
{
    private readonly AtlasDbContext _db;

    public AzureWorkItemLinkRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AzureWorkItemLink>> GetByWorkItemIdsAsync(IReadOnlyList<Guid> workItemIds, CancellationToken cancellationToken = default)
    {
        if (workItemIds.Count == 0) return [];

        return await _db.AzureWorkItemLinks
            .Where(x => workItemIds.Contains(x.AzureWorkItemId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AzureWorkItemLink link, CancellationToken cancellationToken = default)
    {
        await _db.AzureWorkItemLinks.AddAsync(link, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
