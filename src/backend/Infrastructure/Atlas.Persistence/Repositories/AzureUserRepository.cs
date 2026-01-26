using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class AzureUserRepository : IAzureUserRepository
{
    private readonly AtlasDbContext _db;

    public AzureUserRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AzureUser>> GetByUniqueNamesAsync(IReadOnlyList<string> uniqueNames, CancellationToken cancellationToken = default)
    {
        if (uniqueNames.Count == 0) return [];

        return await _db.AzureUsers
            .Where(x => uniqueNames.Contains(x.UniqueName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AzureUser>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.AzureUsers
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AzureUser user, CancellationToken cancellationToken = default)
    {
        await _db.AzureUsers.AddAsync(user, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
