using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Persistence.UnitOfWork;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AtlasDbContext _db;

    public EfUnitOfWork(AtlasDbContext db)
    {
        _db = db;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(tx);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

