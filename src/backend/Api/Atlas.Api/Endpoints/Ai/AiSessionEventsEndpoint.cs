using System.Text.Json;
using Atlas.Api.DTOs.Ai;
using Atlas.Application.Abstractions.Ai;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Atlas.Api.Endpoints.Ai;

public sealed class AiSessionEventsEndpoint : EndpointWithoutRequest
{
    private readonly IAiSessionStore _store;
    private readonly JsonSerializerOptions _jsonOptions;

    public AiSessionEventsEndpoint(IAiSessionStore store, IOptions<JsonOptions> jsonOptions)
    {
        _store = store;
        _jsonOptions = jsonOptions.Value.SerializerOptions;
    }

    public override void Configure()
    {
        Get("/ai/sessions/{sessionId:guid}/events");
        AllowAnonymous();
        Summary(s => { s.Summary = "Stream AI session events over SSE"; });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!Route<Guid?>("sessionId").HasValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        Guid sessionId = Route<Guid>("sessionId");
        bool exists = await _store.ExistsAsync(sessionId, ct);
        if (!exists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        HttpResponse res = HttpContext.Response;
        res.StatusCode = 200;
        res.Headers.ContentType = "text/event-stream";
        res.Headers.CacheControl = "no-cache";
        res.Headers.Connection = "keep-alive";
        res.Headers["X-Accel-Buffering"] = "no";

        await res.Body.FlushAsync(ct);

        await foreach (AiSessionEvent evt in _store.StreamEventsAsync(sessionId, ct))
        {
            var dto = new AiSessionEventDto(
                EventId: evt.EventId,
                SessionId: evt.SessionId,
                Sequence: evt.Sequence,
                Type: evt.Type,
                Status: evt.Status,
                Message: evt.Message,
                Delta: evt.Delta,
                OccurredAtUtc: evt.OccurredAtUtc,
                IsTerminal: evt.IsTerminal);

            string json = JsonSerializer.Serialize(dto, _jsonOptions);
            await res.WriteAsync($"event: {evt.Type}\n", ct);
            await res.WriteAsync($"data: {json}\n\n", ct);
            await res.Body.FlushAsync(ct);
        }
    }
}

