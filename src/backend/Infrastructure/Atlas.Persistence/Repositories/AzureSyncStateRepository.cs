using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class AzureSyncStateRepository : IAzureSyncStateRepository
{
    private readonly AtlasDbContext _db;

    public AzureSyncStateRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<AzureSyncState?> GetByConnectionIdAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        return _db.AzureSyncStates.FirstOrDefaultAsync(x => x.AzureConnectionId == connectionId, cancellationToken);
    }

    public async Task AddAsync(AzureSyncState state, CancellationToken cancellationToken = default)
    {
        await _db.AzureSyncStates.AddAsync(state, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
