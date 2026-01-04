using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IRiskRepository
{
    Task<Risk?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Risk?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Risk risk, CancellationToken cancellationToken = default);
    void Remove(Risk risk);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

