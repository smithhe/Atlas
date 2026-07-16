using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Atlas.Tests.Functional;

namespace Atlas.Tests.Functional.Endpoints.Health;

public sealed class HealthEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk_WhenDatabaseIsReachable()
    {
        HttpResponseMessage response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        HealthResponse? body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
    }

    private sealed record HealthResponse(string Status);
}
