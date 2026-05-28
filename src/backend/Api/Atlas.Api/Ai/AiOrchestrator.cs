using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Ai;
using Atlas.Domain.Entities;
using AppAiSessionEvent = Atlas.Application.Abstractions.Ai.AiSessionEvent;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Atlas.Api.Ai;

public sealed class AiOrchestrator
{
    private readonly IAiSessionStore _store;
    private readonly IAiSessionRepository _sessions;
    private readonly IAiConversationRepository _conversations;
    private readonly IUnitOfWork _uow;
    private readonly AiPromptContextResolver _contextResolver;
    private readonly IChatModelClient _modelClient;
    private readonly AiExecutionGate _executionGate;
    private readonly AiOptions _options;
    private readonly ILogger<AiOrchestrator> _logger;

    public AiOrchestrator(
        IAiSessionStore store,
        IAiSessionRepository sessions,
        IAiConversationRepository conversations,
        IUnitOfWork uow,
        AiPromptContextResolver contextResolver,
        IChatModelClient modelClient,
        AiExecutionGate executionGate,
        IOptions<AiOptions> options,
        ILogger<AiOrchestrator> logger)
    {
        _store = store;
        _sessions = sessions;
        _conversations = conversations;
        _uow = uow;
        _contextResolver = contextResolver;
        _modelClient = modelClient;
        _executionGate = executionGate;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunSessionAsync(Guid sessionId, AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        await _store.PublishEventAsync(new AppAiSessionEvent(
            EventId: Guid.Empty,
            SessionId: sessionId,
            Sequence: 0,
            Type: "session.started",
            OccurredAtUtc: startedAt,
            Status: "started",
            Message: "Session started."), cancellationToken);

        try
        {
            IReadOnlyList<AiChatMessage> messages;
            if (request.TurnIndex == 0)
            {
                await _store.PublishEventAsync(new AppAiSessionEvent(
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
                string composedPrompt = AiConversationMessageBuilder.BuildComposedUserPrompt(request, context, userPrompt);
                messages = AiConversationMessageBuilder.BuildTurnZeroMessages(_options.SystemPrompt, composedPrompt);
            }
            else
            {
                await _store.PublishEventAsync(new AppAiSessionEvent(
                    EventId: Guid.Empty,
                    SessionId: sessionId,
                    Sequence: 0,
                    Type: "history.loading",
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    Status: "using_history",
                    Message: "Using conversation history."), cancellationToken);

                IReadOnlyList<AiSession> priorTurns = await _sessions.ListByConversationIdWithEventsAsync(request.ConversationId, cancellationToken);
                var history = priorTurns
                    .Where(t => t.TurnIndex < request.TurnIndex && t.IsTerminal)
                    .Select(t => new AiConversationTurnHistory(t.Prompt, ExtractAssistantResponse(t.Events)))
                    .ToList();

                string userPrompt = TrimToMax(request.Prompt, _options.MaxPromptChars);
                messages = AiConversationMessageBuilder.BuildFollowUpMessages(
                    _options.SystemPrompt,
                    history,
                    userPrompt,
                    _options.MaxContextChars);
            }

            await _store.PublishEventAsync(new AppAiSessionEvent(
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
                               Messages: messages), cancellationToken))
            {
                if (string.IsNullOrEmpty(delta))
                {
                    continue;
                }

                await _store.PublishEventAsync(new AppAiSessionEvent(
                    EventId: Guid.Empty,
                    SessionId: sessionId,
                    Sequence: 0,
                    Type: "model.delta",
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    Status: "streaming",
                    Delta: delta), cancellationToken);
            }

            TimeSpan elapsed = DateTimeOffset.UtcNow - startedAt;
            await _store.PublishEventAsync(new AppAiSessionEvent(
                EventId: Guid.Empty,
                SessionId: sessionId,
                Sequence: 0,
                Type: "session.completed",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Status: "completed",
                Message: $"Completed in {elapsed.TotalSeconds:F1}s.",
                IsTerminal: true), cancellationToken);

            await _conversations.TouchUpdatedAtAsync(request.ConversationId, DateTimeOffset.UtcNow, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await _store.PublishEventAsync(new AppAiSessionEvent(
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
            await _store.PublishEventAsync(new AppAiSessionEvent(
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

    private static string ExtractAssistantResponse(IEnumerable<Atlas.Domain.Entities.AiSessionEvent> events)
    {
        return string.Concat(events
            .OrderBy(e => e.Sequence)
            .Where(e => e.Type == "model.delta" && !string.IsNullOrEmpty(e.Delta))
            .Select(e => e.Delta));
    }

    private static string TrimToMax(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
        {
            return value;
        }

        return value[..maxChars];
    }
}
