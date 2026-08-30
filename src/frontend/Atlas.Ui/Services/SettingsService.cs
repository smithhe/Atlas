using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Settings mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class SettingsService
{
    private readonly IAtlasApiClient _api;
    private readonly AppCacheService _cache;

    public SettingsService(IAtlasApiClient api, AppCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task UpdateAsync(Settings settings, CancellationToken cancellationToken = default)
    {
        await _api.AtlasApiEndpointsSettingsUpdateSettingsEndpointAsync(
            EntityRequestMappers.ToUpdateSettingsRequest(settings),
            cancellationToken);
        await _cache.RefetchSettingsAsync(cancellationToken);
    }
}
