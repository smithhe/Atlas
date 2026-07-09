using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Ai;
using Atlas.Application.Abstractions.Ai;

namespace Atlas.Tests.Functional.Endpoints.Ai;

public sealed class AiEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AiEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListAiConversations_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/ai/conversations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateAiConversation_ReturnsAccepted()
    {
        CreateAiConversationRequest request = new(
            Prompt: "Summarize open risks",
            View: AiViewScope.Dashboard,
            ActionId: null,
            TaskId: null,
            ProjectId: null,
            RiskId: null,
            TeamMemberId: null);

        HttpResponseMessage response = await _client.PostJsonAsync("/ai/conversations", request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        CreateAiConversationResponse? created = await response.ReadJsonAsync<CreateAiConversationResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.ConversationId);
        Assert.NotEqual(Guid.Empty, created.TurnSessionId);
    }

    [Fact]
    public async Task GetAiConversation_WhenMissing_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync($"/ai/conversations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
