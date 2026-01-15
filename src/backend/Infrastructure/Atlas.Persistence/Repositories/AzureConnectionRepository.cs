using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class AzureConnectionRepository : IAzureConnectionRepository
{
    private readonly AtlasDbContext _db;

    public AzureConnectionRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<AzureConnection?> GetSingletonAsync(CancellationToken cancellationToken = default)
    {
        return _db.AzureConnections.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(AzureConnection connection, CancellationToken cancellationToken = default)
    {
        await _db.AzureConnections.AddAsync(connection, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
