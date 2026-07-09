using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Persistence;
using Atlas.Tests.Common.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atlas.Tests.Common.TestHost;

public static class AtlasTestServiceConfigurator
{
    /// <summary>
    /// Resolves the Integration/Functional DB provider.
    /// When <c>ConnectionStrings__AtlasDb</c> or <c>ATLAS_TEST_DB</c> is set, uses Npgsql (CI Postgres).
    /// Otherwise defaults to EF InMemory for local runs.
    /// </summary>
    public static string? ResolvePostgresConnectionString()
    {
        string? fromConnectionStrings = Environment.GetEnvironmentVariable("ConnectionStrings__AtlasDb");
        if (!string.IsNullOrWhiteSpace(fromConnectionStrings))
        {
            return fromConnectionStrings.Trim();
        }

        string? fromAtlasTestDb = Environment.GetEnvironmentVariable("ATLAS_TEST_DB");
        if (!string.IsNullOrWhiteSpace(fromAtlasTestDb))
        {
            return fromAtlasTestDb.Trim();
        }

        return null;
    }

    public static void ConfigureInMemoryAtlas(IServiceCollection services, string databaseName)
    {
        ConfigureAtlasPersistence(services, databaseName, postgresConnectionString: null);
    }

    public static void ConfigureAtlasPersistence(
        IServiceCollection services,
        string databaseName,
        string? postgresConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            services.AddDbContext<AtlasDbContext>(options =>
                options.UseNpgsql(
                    postgresConnectionString,
                    npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        }
        else
        {
            services.AddDbContext<AtlasDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();
        }

        services.RemoveAll<IAzureDevOpsClient>();
        services.AddSingleton<FakeAzureDevOpsClient>();
        services.AddSingleton<IAzureDevOpsClient>(sp => sp.GetRequiredService<FakeAzureDevOpsClient>());

        services.RemoveAll<IAiConversationService>();
        services.AddSingleton<IAiConversationService, TestAiConversationService>();
    }
}
