using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{

    /// <summary>Settings mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
    public sealed class SettingsService : ISettingsService
    {
        private readonly IAtlasApiClient _api;
        private readonly IAppCacheService _cache;

        public SettingsService(IAtlasApiClient api, IAppCacheService cache)
        {
            _api = api;
            _cache = cache;
        }

        public void PatchLocal(Settings patch) => _cache.PatchSettings(patch);

        public async Task UpdateAsync(Settings settings, CancellationToken cancellationToken = default)
        {
            await _api.AtlasApiEndpointsSettingsUpdateSettingsEndpointAsync(
                EntityRequestMappers.ToUpdateSettingsRequest(settings),
                cancellationToken);

            bool defaultAiPanelOpen = _cache.Settings?.DefaultAiPanelOpen ?? settings.DefaultAiPanelOpen;
            _cache.PatchSettings(new Settings
            {
                StaleDays = settings.StaleDays,
                DefaultAiManualOnly = settings.DefaultAiManualOnly,
                DefaultAiPanelOpen = defaultAiPanelOpen,
                Theme = settings.Theme,
                AzureDevOpsBaseUrl = settings.AzureDevOpsBaseUrl
            });
        }
    }
}
