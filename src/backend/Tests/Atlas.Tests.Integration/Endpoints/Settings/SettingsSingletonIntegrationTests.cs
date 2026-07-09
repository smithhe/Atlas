using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Settings;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Integration.Endpoints.Settings;

public sealed class SettingsSingletonIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly HttpClient _client;

    public SettingsSingletonIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Settings_UpdateIsVisibleOnSubsequentGets()
    {
        HttpResponseMessage updateResponse = await _client.PutJsonAsync("/settings", new UpdateSettingsRequest(
            StaleDays: 30,
            DefaultAiManualOnly: true,
            Theme: Theme.Light,
            AzureDevOpsBaseUrl: "https://dev.azure.com/test-org"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        SettingsDto? first = await (await _client.GetAsync("/settings")).ReadJsonAsync<SettingsDto>();
        SettingsDto? second = await (await _client.GetAsync("/settings")).ReadJsonAsync<SettingsDto>();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(30, first.StaleDays);
        Assert.True(second.DefaultAiManualOnly);
        Assert.Equal("https://dev.azure.com/test-org", second.AzureDevOpsBaseUrl);
    }
}
