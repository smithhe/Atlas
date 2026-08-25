using Atlas.Ui.Api.Generated;

namespace Atlas.Ui.Services;

/// <summary>Settings mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class SettingsService
{
    private readonly IAtlasApiClient _api;

    public SettingsService(IAtlasApiClient api)
    {
        _api = api;
    }

    public Task UpdateAsync(AtlasApiDTOsSettingsUpdateSettingsRequest request, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsSettingsUpdateSettingsEndpointAsync(request, cancellationToken);
}
