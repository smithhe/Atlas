using System.Net;
using System.Net.Http;

namespace Atlas.Tests.Functional.Endpoints.ProductOwners;

public sealed class ProductOwnersEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductOwnersEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListProductOwners_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/product-owners");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
