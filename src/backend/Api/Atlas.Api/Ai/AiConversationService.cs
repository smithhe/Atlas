using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;
using AppAiSessionEvent = Atlas.Application.Abstractions.Ai.AiSessionEvent;
using Microsoft.Extensions.Logging;

namespace Atlas.Api.Ai;

public sealed class AiConversationService : IAiConversationService
{
    private readonly IAiSessionStore _store;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiConversationService> _logger;

    public AiConversationService(
        IAiSessionStore store,
        IServiceScopeFactory scopeFactory,
        ILogger<AiConversationService> logger)
    {
        _store = store;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<(Guid ConversationId, Guid TurnSessionId)> StartConversationAsync(
        AiSessionStartRequest request,
        CancellationToken cancellationToken)
    {
        Guid conversationId = request.ConversationId;
        var sessionId = Guid.NewGuid();

        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            IAiConversationRepository conversations = scope.ServiceProvider.GetRequiredService<IAiConversationRepository>();
            IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            var conversation = new AiConversation
            {
                Id = conversationId,
                Title = BuildTitle(request.Prompt),
                View = request.View.ToString(),
                ActionId = request.ActionId,
                TaskId = request.TaskId,
                ProjectId = request.ProjectId,
                RiskId = request.RiskId,
                TeamMemberId = request.TeamMemberId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            await conversations.AddAsync(conversation, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);
        }

        AiSessionStartRequest turnRequest = request with { TurnIndex = 0 };
        await _store.CreateTurnAsync(sessionId, turnRequest, cancellationToken);
        StartWorker(sessionId, turnRequest);

        return (conversationId, sessionId);
    }

    public async Task<Guid> ContinueConversationAsync(Guid conversationId, string prompt, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAiConversationRepository conversations = scope.ServiceProvider.GetRequiredService<IAiConversationRepository>();
        IAiSessionRepository sessions = scope.ServiceProvider.GetRequiredService<IAiSessionRepository>();

        AiConversation? conversation = await conversations.GetByIdWithTurnsAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            throw new InvalidOperationException("Conversation not found.");
        }

        AiSession? latestTurn = await sessions.GetLatestTurnAsync(conversationId, cancellationToken);
        if (latestTurn is not null && !latestTurn.IsTerminal)
        {
            throw new InvalidOperationException("A turn is still in progress for this conversation.");
        }

        var turnIndex = await sessions.GetNextTurnIndexAsync(conversationId, cancellationToken);
        var sessionId = Guid.NewGuid();
        var trimmedPrompt = prompt.Trim();

        var turnRequest = new AiSessionStartRequest(
            ConversationId: conversationId,
            TurnIndex: turnIndex,
            Prompt: trimmedPrompt,
            View: Enum.Parse<AiViewScope>(conversation.View, ignoreCase: true),
            ActionId: conversation.ActionId,
            TaskId: conversation.TaskId,
            ProjectId: conversation.ProjectId,
            RiskId: conversation.RiskId,
            TeamMemberId: conversation.TeamMemberId);

        await _store.CreateTurnAsync(sessionId, turnRequest, cancellationToken);
        StartWorker(sessionId, turnRequest);

        return sessionId;
    }

    private void StartWorker(Guid sessionId, AiSessionStartRequest request)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                AiOrchestrator orchestrator = scope.ServiceProvider.GetRequiredService<AiOrchestrator>();
                await orchestrator.RunSessionAsync(sessionId, request, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start AI turn worker for {SessionId}", sessionId);
                await _store.PublishEventAsync(new AppAiSessionEvent(
                    EventId: Guid.Empty,
                    SessionId: sessionId,
                    Sequence: 0,
                    Type: "session.failed",
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    Status: "failed",
                    Message: "Unable to start session worker.",
                    IsTerminal: true), CancellationToken.None);
            }
        }, CancellationToken.None);
    }

    private static string BuildTitle(string prompt)
    {
        var trimmed = prompt.Trim();
        if (trimmed.Length <= 80)
        {
            return trimmed;
        }

        return trimmed[..80];
    }
}
