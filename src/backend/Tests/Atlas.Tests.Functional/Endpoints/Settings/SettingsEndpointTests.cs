using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Settings;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.Settings;

public sealed class SettingsEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SettingsEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Settings_GetAndUpdate_Works()
    {
        HttpResponseMessage getResponse = await _client.GetAsync("/settings");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        SettingsDto? initial = await getResponse.ReadJsonAsync<SettingsDto>();
        Assert.NotNull(initial);
        Assert.InRange(initial.StaleDays, 1, 365);
        Assert.Equal(Theme.Dark, initial.Theme);

        UpdateSettingsRequest updateRequest = new(
            StaleDays: 21,
            DefaultAiManualOnly: false,
            Theme: Theme.Light,
            AzureDevOpsBaseUrl: "https://dev.azure.com/example");

        HttpResponseMessage updateResponse = await _client.PutJsonAsync("/settings", updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        HttpResponseMessage updatedGetResponse = await _client.GetAsync("/settings");
        SettingsDto? updated = await updatedGetResponse.ReadJsonAsync<SettingsDto>();

        Assert.NotNull(updated);
        Assert.Equal(21, updated.StaleDays);
        Assert.False(updated.DefaultAiManualOnly);
        Assert.Equal(Theme.Light, updated.Theme);
        Assert.Equal("https://dev.azure.com/example", updated.AzureDevOpsBaseUrl);
    }
}
