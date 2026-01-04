using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface ISettingsRepository
{
    Task<Settings?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atlas settings are expected to be a singleton row.
    /// </summary>
    Task<Settings?> GetSingletonAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Settings settings, CancellationToken cancellationToken = default);
    void Remove(Settings settings);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

