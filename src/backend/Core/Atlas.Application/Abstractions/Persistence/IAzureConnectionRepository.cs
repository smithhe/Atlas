using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAzureConnectionRepository
{
    Task<AzureConnection?> GetSingletonAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AzureConnection connection, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
