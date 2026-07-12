using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Tests.Common.Fakes;

public sealed class TestAiConversationService : IAiConversationService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TestAiConversationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<(Guid ConversationId, Guid TurnSessionId)> StartConversationAsync(
        AiSessionStartRequest request,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAiConversationRepository conversations = scope.ServiceProvider.GetRequiredService<IAiConversationRepository>();
        IAiSessionRepository sessions = scope.ServiceProvider.GetRequiredService<IAiSessionRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        await conversations.AddAsync(new AiConversation
        {
            Id = request.ConversationId,
            Title = BuildTitle(request.Prompt),
            View = request.View.ToString(),
            ActionId = request.ActionId,
            TaskId = request.TaskId,
            ProjectId = request.ProjectId,
            RiskId = request.RiskId,
            TeamMemberId = request.TeamMemberId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        await sessions.AddAsync(CreateTerminalSession(sessionId, request), cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return (request.ConversationId, sessionId);
    }

    public async Task<Guid> ContinueConversationAsync(Guid conversationId, string prompt, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAiConversationRepository conversations = scope.ServiceProvider.GetRequiredService<IAiConversationRepository>();
        IAiSessionRepository sessions = scope.ServiceProvider.GetRequiredService<IAiSessionRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

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

        int turnIndex = await sessions.GetNextTurnIndexAsync(conversationId, cancellationToken);
        var sessionId = Guid.NewGuid();
        string trimmedPrompt = prompt.Trim();

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

        await sessions.AddAsync(CreateTerminalSession(sessionId, turnRequest), cancellationToken);
        await conversations.TouchUpdatedAtAsync(conversationId, DateTimeOffset.UtcNow, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        return sessionId;
    }

    private static AiSession CreateTerminalSession(Guid sessionId, AiSessionStartRequest request)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new AiSession
        {
            Id = sessionId,
            ConversationId = request.ConversationId,
            TurnIndex = request.TurnIndex,
            Title = BuildTitle(request.Prompt),
            Prompt = request.Prompt,
            View = request.View.ToString(),
            ActionId = request.ActionId,
            TaskId = request.TaskId,
            ProjectId = request.ProjectId,
            RiskId = request.RiskId,
            TeamMemberId = request.TeamMemberId,
            CreatedAtUtc = now,
            CompletedAtUtc = now,
            Status = "completed",
            IsTerminal = true
        };
    }

    private static string BuildTitle(string prompt)
    {
        string trimmed = prompt.Trim();
        return trimmed.Length <= 80 ? trimmed : trimmed[..80];
    }
}
