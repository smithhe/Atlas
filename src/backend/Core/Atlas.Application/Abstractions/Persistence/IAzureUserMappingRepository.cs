using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAzureUserMappingRepository
{
    Task<IReadOnlyList<AzureUserMapping>> GetByUniqueNamesAsync(IReadOnlyList<string> uniqueNames, CancellationToken cancellationToken = default);
    Task AddAsync(AzureUserMapping mapping, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
