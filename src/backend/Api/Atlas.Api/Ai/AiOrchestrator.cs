using Atlas.Application.Abstractions.Ai;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Atlas.Api.Ai;

public sealed class AiOrchestrator
{
    private readonly IAiSessionStore _store;
    private readonly AiPromptContextResolver _contextResolver;
    private readonly IChatModelClient _modelClient;
    private readonly AiExecutionGate _executionGate;
    private readonly AiOptions _options;
    private readonly ILogger<AiOrchestrator> _logger;

    public AiOrchestrator(
        IAiSessionStore store,
        AiPromptContextResolver contextResolver,
        IChatModelClient modelClient,
        AiExecutionGate executionGate,
        IOptions<AiOptions> options,
        ILogger<AiOrchestrator> logger)
    {
        _store = store;
        _contextResolver = contextResolver;
        _modelClient = modelClient;
        _executionGate = executionGate;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunSessionAsync(Guid sessionId, AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        await _store.PublishEventAsync(new AiSessionEvent(
            EventId: Guid.Empty,
            SessionId: sessionId,
            Sequence: 0,
            Type: "session.started",
            OccurredAtUtc: startedAt,
            Status: "started",
            Message: "Session started."), cancellationToken);

        try
        {
            await _store.PublishEventAsync(new AiSessionEvent(
                EventId: Guid.Empty,
                SessionId: sessionId,
                Sequence: 0,
                Type: "context.gathering",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Status: "gathering_context",
                Message: "Gathering context."), cancellationToken);

            string context = await _contextResolver.BuildContextAsync(request, cancellationToken);
            context = TrimToMax(context, _options.MaxContextChars);

            string userPrompt = TrimToMax(request.Prompt, _options.MaxPromptChars);
            string composedPrompt = BuildUserPrompt(request, context, userPrompt);

            await _store.PublishEventAsync(new AiSessionEvent(
                EventId: Guid.Empty,
                SessionId: sessionId,
                Sequence: 0,
                Type: "model.requested",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Status: "model_requested",
                Message: "Calling model."), cancellationToken);

            using IDisposable _ = await _executionGate.EnterAsync(cancellationToken);

            await foreach (string delta in _modelClient.GenerateStreamingAsync(new AiModelRequest(
                               SystemPrompt: _options.SystemPrompt,
                               UserPrompt: composedPrompt), cancellationToken))
            {
                if (string.IsNullOrEmpty(delta))
                {
                    continue;
                }

                await _store.PublishEventAsync(new AiSessionEvent(
                    EventId: Guid.Empty,
                    SessionId: sessionId,
                    Sequence: 0,
                    Type: "model.delta",
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    Status: "streaming",
                    Delta: delta), cancellationToken);
            }

            TimeSpan elapsed = DateTimeOffset.UtcNow - startedAt;
            await _store.PublishEventAsync(new AiSessionEvent(
                EventId: Guid.Empty,
                SessionId: sessionId,
                Sequence: 0,
                Type: "session.completed",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Status: "completed",
                Message: $"Completed in {elapsed.TotalSeconds:F1}s.",
                IsTerminal: true), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await _store.PublishEventAsync(new AiSessionEvent(
                EventId: Guid.Empty,
                SessionId: sessionId,
                Sequence: 0,
                Type: "session.failed",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Status: "cancelled",
                Message: "Session cancelled.",
                IsTerminal: true), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI session {SessionId} failed", sessionId);
            await _store.PublishEventAsync(new AiSessionEvent(
                EventId: Guid.Empty,
                SessionId: sessionId,
                Sequence: 0,
                Type: "session.failed",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Status: "failed",
                Message: "Unable to complete AI request.",
                IsTerminal: true), CancellationToken.None);
        }
    }

    private static string TrimToMax(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
        {
            return value;
        }

        return value[..maxChars];
    }

    private static string BuildUserPrompt(AiSessionStartRequest request, string context, string userPrompt)
    {
        return
            $"Atlas view: {request.View}\n" +
            $"Action: {(string.IsNullOrWhiteSpace(request.ActionId) ? "none" : request.ActionId)}\n\n" +
            "Context:\n" +
            $"{context}\n\n" +
            "User request:\n" +
            $"{userPrompt}";
    }
}

