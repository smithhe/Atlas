using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Ai;
using Atlas.Application.Abstractions.Ai;

namespace Atlas.Tests.Functional.Endpoints.Ai;

public sealed class AiContinueEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AiContinueEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ContinueAiConversation_WhenConversationExists_ReturnsAccepted()
    {
        CreateAiConversationResponse? created = await (await _client.PostJsonAsync(
            "/ai/conversations",
            new CreateAiConversationRequest(
                Prompt: "Summarize tasks",
                View: AiViewScope.Tasks,
                ActionId: null,
                TaskId: null,
                ProjectId: null,
                RiskId: null,
                TeamMemberId: null))).ReadJsonAsync<CreateAiConversationResponse>();
        Assert.NotNull(created);

        HttpResponseMessage response = await _client.PostJsonAsync(
            $"/ai/conversations/{created.ConversationId}/messages",
            new ContinueAiConversationRequest { Prompt = "What changed?" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        ContinueAiConversationResponse? continued = await response.ReadJsonAsync<ContinueAiConversationResponse>();
        Assert.NotNull(continued);
        Assert.NotEqual(Guid.Empty, continued.TurnSessionId);
    }

    [Fact]
    public async Task ContinueAiConversation_WhenMissing_ReturnsConflict()
    {
        HttpResponseMessage response = await _client.PostJsonAsync(
            $"/ai/conversations/{Guid.NewGuid()}/messages",
            new ContinueAiConversationRequest { Prompt = "Hello" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAiConversation_AfterCreate_ReturnsDetail()
    {
        CreateAiConversationResponse? created = await (await _client.PostJsonAsync(
            "/ai/conversations",
            new CreateAiConversationRequest(
                Prompt: "Dashboard summary",
                View: AiViewScope.Dashboard,
                ActionId: null,
                TaskId: null,
                ProjectId: null,
                RiskId: null,
                TeamMemberId: null))).ReadJsonAsync<CreateAiConversationResponse>();
        Assert.NotNull(created);

        AiConversationDetailDto? detail = await (await _client.GetAsync($"/ai/conversations/{created.ConversationId}"))
            .ReadJsonAsync<AiConversationDetailDto>();

        Assert.NotNull(detail);
        Assert.Equal(created.ConversationId, detail.ConversationId);
        Assert.Equal(AiViewScope.Dashboard, detail.View);
    }
}
