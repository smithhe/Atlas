using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class SettingsRepository : ISettingsRepository
{
    private readonly AtlasDbContext _db;

    public SettingsRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<Settings?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Settings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Settings?> GetSingletonAsync(CancellationToken cancellationToken = default)
    {
        return _db.Settings.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Settings settings, CancellationToken cancellationToken = default)
    {
        await _db.Settings.AddAsync(settings, cancellationToken);
    }

    public void Remove(Settings settings)
    {
        _db.Settings.Remove(settings);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

