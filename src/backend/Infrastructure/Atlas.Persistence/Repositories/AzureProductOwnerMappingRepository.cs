using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class AzureProductOwnerMappingRepository : IAzureProductOwnerMappingRepository
{
    private readonly AtlasDbContext _db;

    public AzureProductOwnerMappingRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AzureProductOwnerMapping>> GetByUniqueNamesAsync(
        IReadOnlyList<string> uniqueNames,
        CancellationToken cancellationToken = default)
    {
        if (uniqueNames.Count == 0)
        {
            return [];
        }

        return await _db.AzureProductOwnerMappings
            .Where(x => uniqueNames.Contains(x.AzureUniqueName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListUniqueNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.AzureProductOwnerMappings
            .Select(x => x.AzureUniqueName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AzureProductOwnerMapping mapping, CancellationToken cancellationToken = default)
    {
        await _db.AzureProductOwnerMappings.AddAsync(mapping, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
