using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAzureProductOwnerMappingRepository
{
    Task<IReadOnlyList<AzureProductOwnerMapping>> GetByUniqueNamesAsync(IReadOnlyList<string> uniqueNames, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListUniqueNamesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AzureProductOwnerMapping mapping, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
