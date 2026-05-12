using Atlas.Application.Abstractions.Ai;
using Microsoft.Extensions.Logging;

namespace Atlas.Api.Ai;

public sealed class AiSessionService : IAiSessionService
{
    private readonly IAiSessionStore _store;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiSessionService> _logger;

    public AiSessionService(
        IAiSessionStore store,
        IServiceScopeFactory scopeFactory,
        ILogger<AiSessionService> logger)
    {
        _store = store;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Guid> StartSessionAsync(AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        Guid sessionId = Guid.NewGuid();
        await _store.CreateSessionAsync(sessionId, request, cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<AiOrchestrator>();
                await orchestrator.RunSessionAsync(sessionId, request, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start AI session worker for {SessionId}", sessionId);
                await _store.PublishEventAsync(new AiSessionEvent(
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

        return sessionId;
    }
}

