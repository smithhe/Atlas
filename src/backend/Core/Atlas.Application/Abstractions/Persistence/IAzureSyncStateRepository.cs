using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAzureSyncStateRepository
{
    Task<AzureSyncState?> GetByConnectionIdAsync(Guid connectionId, CancellationToken cancellationToken = default);
    Task AddAsync(AzureSyncState state, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
