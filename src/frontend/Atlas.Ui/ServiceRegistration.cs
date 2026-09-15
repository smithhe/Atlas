using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Services;

namespace Atlas.Ui
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddAtlasUiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            string apiBaseUrl = (configuration["ApiBaseUrl"] ?? "http://localhost:5012").TrimEnd('/');

            services.AddScoped(_ => new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl + "/")
            });

            services.AddScoped<IAtlasApiClient>(sp =>
            {
                HttpClient http = sp.GetRequiredService<HttpClient>();
                return new AtlasApiClient(apiBaseUrl, http);
            });

            services.AddScoped<SelectionState>();
            services.AddScoped<LocalSettings>();
            services.AddScoped<BrowserDialogs>();
            services.AddScoped<MarkdownRenderer>();
            services.AddScoped<AiSessionEventsClient>();

            services.AddScoped<IAppCacheService, AppCacheService>();
            services.AddScoped<IAiStateService, AiStateService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IRiskService, RiskService>();
            services.AddScoped<ITeamMemberService, TeamMemberService>();
            services.AddScoped<ITeamNoteService, TeamNoteService>();
            services.AddScoped<ITeamMemberRiskService, TeamMemberRiskService>();
            services.AddScoped<IGrowthService, GrowthService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<IAzureDevOpsService, AzureDevOpsService>();
            services.AddScoped<IAzureWorkItemService, AzureWorkItemService>();

            return services;
        }
    }
}
