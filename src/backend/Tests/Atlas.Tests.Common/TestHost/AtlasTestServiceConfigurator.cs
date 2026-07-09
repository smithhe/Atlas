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
    public static void ConfigureInMemoryAtlas(IServiceCollection services, string databaseName)
    {
        services.AddDbContext<AtlasDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        services.RemoveAll<IUnitOfWork>();
        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();

        services.RemoveAll<IAzureDevOpsClient>();
        services.AddSingleton<FakeAzureDevOpsClient>();
        services.AddSingleton<IAzureDevOpsClient>(sp => sp.GetRequiredService<FakeAzureDevOpsClient>());

        services.RemoveAll<IAiConversationService>();
        services.AddSingleton<IAiConversationService, TestAiConversationService>();
    }
}
