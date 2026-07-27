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

// NOTE (Phase 5): Atlas create endpoints return HTTP 201. The committed generated client
// treats `status_ == 200 || status_ == 201` as success. Re-apply after regenerate until
// openapi/atlas.v1.json declares 201 for create operations (or NSwag operation settings cover it).
