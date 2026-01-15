using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class AzureUserMappingRepository : IAzureUserMappingRepository
{
    private readonly AtlasDbContext _db;

    public AzureUserMappingRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AzureUserMapping>> GetByUniqueNamesAsync(IReadOnlyList<string> uniqueNames, CancellationToken cancellationToken = default)
    {
        if (uniqueNames.Count == 0) return [];

        return await _db.AzureUserMappings
            .Where(x => uniqueNames.Contains(x.AzureUniqueName))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AzureUserMapping mapping, CancellationToken cancellationToken = default)
    {
        await _db.AzureUserMappings.AddAsync(mapping, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
