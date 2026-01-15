using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAzureWorkItemLinkRepository
{
    Task<IReadOnlyList<AzureWorkItemLink>> GetByWorkItemIdsAsync(IReadOnlyList<Guid> workItemIds, CancellationToken cancellationToken = default);
    Task AddAsync(AzureWorkItemLink link, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
