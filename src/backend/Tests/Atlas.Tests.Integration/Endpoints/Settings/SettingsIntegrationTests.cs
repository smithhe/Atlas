using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Settings;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Integration.Endpoints.Settings;

public sealed class SettingsIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly HttpClient _client;

    public SettingsIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HttpPipeline_GetSettings_CreatesSingletonWhenMissing()
    {
        HttpResponseMessage response = await _client.GetAsync("/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettingsDto? settings = await response.ReadJsonAsync<SettingsDto>();
        Assert.NotNull(settings);
        Assert.True(settings.StaleDays > 0);
        Assert.Equal(Theme.Dark, settings.Theme);
    }
}
