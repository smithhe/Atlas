using Atlas.Application.Abstractions.Persistence;
using Atlas.Persistence.Repositories;
using Atlas.Persistence.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence services assuming the caller has already registered <see cref="AtlasDbContext"/>.
    /// </summary>
    public static IServiceCollection AddAtlasPersistence(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IRiskRepository, RiskRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IGrowthRepository, GrowthRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IProductOwnerRepository, ProductOwnerRepository>();
        return services;
    }
}

