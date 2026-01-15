using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAzureUserRepository
{
    Task<IReadOnlyList<AzureUser>> GetByUniqueNamesAsync(IReadOnlyList<string> uniqueNames, CancellationToken cancellationToken = default);
    Task AddAsync(AzureUser user, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
