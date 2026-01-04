namespace Atlas.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    /// <summary>
    /// Begins an explicit transaction for a use-case boundary (typically a command).
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes in the current unit-of-work.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

