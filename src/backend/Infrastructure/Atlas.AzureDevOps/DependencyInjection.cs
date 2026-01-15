namespace Atlas.AzureDevOps;

public static class DependencyInjection
{
    public static IServiceCollection AddAzureDevOps(this IServiceCollection services)
    {
        services.AddHttpClient<IAzureDevOpsClient, AzureDevOpsClient>();
        return services;
    }
}
