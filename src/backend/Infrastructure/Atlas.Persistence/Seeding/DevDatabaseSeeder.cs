using Microsoft.EntityFrameworkCore;

namespace Atlas.Persistence.Seeding;

public static class DevDatabaseSeeder
{
    public static async Task SeedAsync(AtlasDbContext db, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }
}

