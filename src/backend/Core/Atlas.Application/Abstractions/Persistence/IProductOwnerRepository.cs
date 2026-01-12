using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IProductOwnerRepository
{
    Task<ProductOwner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductOwner>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ProductOwner owner, CancellationToken cancellationToken = default);
    void Remove(ProductOwner owner);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

