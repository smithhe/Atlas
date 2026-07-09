using Atlas.Persistence;
using Atlas.Tests.Common.Fakes;
using Atlas.Tests.Common.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atlas.Tests.Integration;

public sealed class AtlasIntegrationApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _inMemoryDatabaseName = $"AtlasIntegrationTests-{Guid.NewGuid():N}";
    private readonly string? _basePostgresConnectionString = AtlasTestServiceConfigurator.ResolvePostgresConnectionString();
    private readonly string? _isolatedPostgresDatabaseName;
    private readonly string? _isolatedPostgresConnectionString;

    public AtlasIntegrationApplicationFactory()
    {
        if (!string.IsNullOrWhiteSpace(_basePostgresConnectionString))
        {
            // Unique DB per factory instance so parallel/class fixtures never share state.
            _isolatedPostgresDatabaseName = $"atlas_it_{Guid.NewGuid():N}";
            PostgresTestDatabase.EnsureDatabaseExists(_basePostgresConnectionString, _isolatedPostgresDatabaseName);
            _isolatedPostgresConnectionString = PostgresTestDatabase.BuildIsolatedConnectionString(
                _basePostgresConnectionString,
                _isolatedPostgresDatabaseName);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            AtlasTestServiceConfigurator.ConfigureAtlasPersistence(
                services,
                _inMemoryDatabaseName,
                _isolatedPostgresConnectionString);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        IHost host = base.CreateHost(builder);

        using IServiceScope scope = host.Services.CreateScope();
        AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing
            && !string.IsNullOrWhiteSpace(_basePostgresConnectionString)
            && !string.IsNullOrWhiteSpace(_isolatedPostgresDatabaseName))
        {
            PostgresTestDatabase.DropDatabase(_basePostgresConnectionString, _isolatedPostgresDatabaseName);
        }
    }

    public FakeAzureDevOpsClient AzureDevOps => Services.GetRequiredService<FakeAzureDevOpsClient>();

    public bool UsesPostgres => !string.IsNullOrWhiteSpace(_isolatedPostgresConnectionString);
}
