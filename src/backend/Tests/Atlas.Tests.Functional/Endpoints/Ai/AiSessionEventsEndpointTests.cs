using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Ai;
using Atlas.Application.Abstractions.Ai;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Tests.Functional.Endpoints.Ai;

public sealed class AiSessionEventsEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly AtlasWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AiSessionEventsEndpointTests(AtlasWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSessionEvents_WhenEventsAlreadyPersisted_ReplaysTerminalEvent()
    {
        CreateAiConversationResponse created = await CreateConversationAsync("Replay events");
        IAiSessionStore store = _factory.Services.GetRequiredService<IAiSessionStore>();

        await store.PublishEventAsync(
            new AiSessionEvent(
                EventId: Guid.NewGuid(),
                SessionId: created.TurnSessionId,
                Sequence: 0,
                Type: "status",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Status: "completed",
                Message: "done",
                Delta: null,
                IsTerminal: true),
            CancellationToken.None);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/ai/sessions/{created.TurnSessionId}/events");
        using HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("event: status");
        body.Should().Contain("\"isTerminal\":true");
    }

    [Fact]
    public async Task GetSessionEvents_WhenConnectedFirst_StreamsLiveTerminalEvent()
    {
        CreateAiConversationResponse created = await CreateConversationAsync("Live stream events");
        IAiSessionStore store = _factory.Services.GetRequiredService<IAiSessionStore>();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/ai/sessions/{created.TurnSessionId}/events");
        using HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        Task<string> readBody = response.Content.ReadAsStringAsync();

        // Give the SSE subscriber a moment to register before publishing.
        await Task.Delay(100);

        await store.PublishEventAsync(
            new AiSessionEvent(
                EventId: Guid.NewGuid(),
                SessionId: created.TurnSessionId,
                Sequence: 0,
                Type: "status",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Status: "completed",
                Message: "live-done",
                Delta: null,
                IsTerminal: true),
            CancellationToken.None);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        string body = await readBody.WaitAsync(cts.Token);

        body.Should().Contain("event: status");
        body.Should().Contain("data:");
        body.Should().Contain("live-done");
        body.Should().Contain("\"isTerminal\":true");
    }

    [Fact]
    public async Task GetSessionEvents_WhenMissing_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync($"/ai/sessions/{Guid.NewGuid()}/events");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<CreateAiConversationResponse> CreateConversationAsync(string prompt)
    {
        CreateAiConversationResponse? created = await (await _client.PostJsonAsync(
            "/ai/conversations",
            new CreateAiConversationRequest(
                Prompt: prompt,
                View: AiViewScope.Dashboard,
                ActionId: null,
                TaskId: null,
                ProjectId: null,
                RiskId: null,
                TeamMemberId: null))).ReadJsonAsync<CreateAiConversationResponse>();
        created.Should().NotBeNull();
        return created;
    }
}
