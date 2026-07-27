namespace Atlas.Ui.Api.Generated;

/// <summary>
/// Partial hooks for the NSwag-generated <see cref="AtlasApiClient"/>.
/// API JSON uses camelCase; OpenAPI schemas use PascalCase property names.
/// </summary>
public partial class AtlasApiClient
{
    static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
    {
        settings.PropertyNameCaseInsensitive = true;
        settings.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }
}
