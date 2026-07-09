using Atlas.Persistence;
using Atlas.Tests.Common.Fakes;
using Atlas.Tests.Common.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atlas.Tests.Functional;

public sealed class AtlasWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AtlasFunctionalTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            AtlasTestServiceConfigurator.ConfigureInMemoryAtlas(services, _databaseName);
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

    public FakeAzureDevOpsClient AzureDevOps => Services.GetRequiredService<FakeAzureDevOpsClient>();
}
