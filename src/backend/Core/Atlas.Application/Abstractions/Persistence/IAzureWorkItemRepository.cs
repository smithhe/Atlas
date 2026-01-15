using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IAzureWorkItemRepository
{
    Task<IReadOnlyList<AzureWorkItem>> GetByWorkItemIdsAsync(Guid connectionId, IReadOnlyList<int> workItemIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AzureWorkItem>> ListUnlinkedAsync(Guid connectionId, int take, CancellationToken cancellationToken = default);
    Task AddAsync(AzureWorkItem workItem, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
