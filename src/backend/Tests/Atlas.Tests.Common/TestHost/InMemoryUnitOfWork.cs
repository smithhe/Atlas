using Atlas.Application.Abstractions.Persistence;
using Atlas.Persistence;

namespace Atlas.Tests.Common.TestHost;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly AtlasDbContext _db;

    public InMemoryUnitOfWork(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IUnitOfWorkTransaction>(new NoOpUnitOfWorkTransaction());

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}

public sealed class NoOpUnitOfWorkTransaction : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
