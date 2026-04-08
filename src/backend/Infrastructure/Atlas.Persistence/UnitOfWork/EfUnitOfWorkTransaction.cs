using Atlas.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Atlas.Persistence.UnitOfWork;

internal sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _tx;

    public EfUnitOfWorkTransaction(IDbContextTransaction tx)
    {
        _tx = tx;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        return _tx.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        return _tx.RollbackAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _tx.DisposeAsync();
    }
}

