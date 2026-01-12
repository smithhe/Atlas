using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class ProductOwnerRepository : IProductOwnerRepository
{
    private readonly AtlasDbContext _db;

    public ProductOwnerRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<ProductOwner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.ProductOwners.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductOwner>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ProductOwners
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductOwner owner, CancellationToken cancellationToken = default)
    {
        await _db.ProductOwners.AddAsync(owner, cancellationToken);
    }

    public void Remove(ProductOwner owner)
    {
        _db.ProductOwners.Remove(owner);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

