using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using DomainAiSession = Atlas.Domain.Entities.AiSession;
using DomainAiSessionEvent = Atlas.Domain.Entities.AiSessionEvent;

namespace Atlas.Api.Ai;

public sealed class PersistentAiSessionStore : IAiSessionStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _gate = new();
    private readonly Dictionary<Guid, List<ChannelWriter<AiSessionEvent>>> _subscribers = [];

    public PersistentAiSessionStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task CreateSessionAsync(Guid sessionId, AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAiSessionRepository sessions = scope.ServiceProvider.GetRequiredService<IAiSessionRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var session = new DomainAiSession
        {
            Id = sessionId,
            Title = BuildTitle(request.Prompt),
            Prompt = request.Prompt,
            View = request.View.ToString(),
            ActionId = request.ActionId,
            TaskId = request.TaskId,
            ProjectId = request.ProjectId,
            RiskId = request.RiskId,
            TeamMemberId = request.TeamMemberId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Status = "created",
            IsTerminal = false,
        };

        await sessions.AddAsync(session, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAiSessionRepository sessions = scope.ServiceProvider.GetRequiredService<IAiSessionRepository>();
        return await sessions.ExistsAsync(sessionId, cancellationToken);
    }

    public async ValueTask PublishEventAsync(AiSessionEvent evt, CancellationToken cancellationToken)
    {
        AiSessionEvent saved = await SaveEventAsync(evt, cancellationToken);
        List<ChannelWriter<AiSessionEvent>> writers;

        lock (_gate)
        {
            writers = _subscribers.TryGetValue(saved.SessionId, out List<ChannelWriter<AiSessionEvent>>? current)
                ? current.ToList()
                : [];
        }

        foreach (ChannelWriter<AiSessionEvent> writer in writers)
        {
            writer.TryWrite(saved);
            if (saved.IsTerminal)
            {
                writer.TryComplete();
            }
        }
    }

    public async IAsyncEnumerable<AiSessionEvent> StreamEventsAsync(
        Guid sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Channel<AiSessionEvent> channel = Channel.CreateUnbounded<AiSessionEvent>();
        var deliveredIds = new HashSet<Guid>();

        IReadOnlyList<AiSessionEvent> replay = await LoadEventsAsync(sessionId, afterSequence: null, cancellationToken);
        int lastSequence = 0;
        bool terminalAlreadyReached = false;

        foreach (AiSessionEvent evt in replay)
        {
            deliveredIds.Add(evt.EventId);
            lastSequence = Math.Max(lastSequence, evt.Sequence);
            terminalAlreadyReached = terminalAlreadyReached || evt.IsTerminal;
            yield return evt;
        }

        if (terminalAlreadyReached)
        {
            yield break;
        }

        lock (_gate)
        {
            if (!_subscribers.TryGetValue(sessionId, out List<ChannelWriter<AiSessionEvent>>? writers))
            {
                writers = [];
                _subscribers[sessionId] = writers;
            }

            writers.Add(channel.Writer);
        }

        IReadOnlyList<AiSessionEvent> catchup = await LoadEventsAsync(sessionId, lastSequence, cancellationToken);
        foreach (AiSessionEvent evt in catchup)
        {
            if (!deliveredIds.Add(evt.EventId))
            {
                continue;
            }

            terminalAlreadyReached = terminalAlreadyReached || evt.IsTerminal;
            yield return evt;
        }

        if (terminalAlreadyReached)
        {
            RemoveSubscriber(sessionId, channel.Writer);
            yield break;
        }

        using CancellationTokenRegistration reg = cancellationToken.Register(() => channel.Writer.TryComplete());

        try
        {
            await foreach (AiSessionEvent evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (!deliveredIds.Add(evt.EventId))
                {
                    continue;
                }

                yield return evt;
            }
        }
        finally
        {
            RemoveSubscriber(sessionId, channel.Writer);
        }
    }

    private async Task<AiSessionEvent> SaveEventAsync(AiSessionEvent evt, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAiSessionRepository sessions = scope.ServiceProvider.GetRequiredService<IAiSessionRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        DomainAiSessionEvent entity = ToEntity(evt);
        DomainAiSessionEvent saved = await sessions.AppendEventAsync(evt.SessionId, entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return ToApplicationEvent(saved);
    }

    private async Task<IReadOnlyList<AiSessionEvent>> LoadEventsAsync(Guid sessionId, int? afterSequence, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IAiSessionRepository sessions = scope.ServiceProvider.GetRequiredService<IAiSessionRepository>();
        IReadOnlyList<DomainAiSessionEvent> events = await sessions.ListEventsAsync(sessionId, afterSequence, cancellationToken);
        return events.Select(ToApplicationEvent).ToList();
    }

    private void RemoveSubscriber(Guid sessionId, ChannelWriter<AiSessionEvent> writer)
    {
        lock (_gate)
        {
            if (!_subscribers.TryGetValue(sessionId, out List<ChannelWriter<AiSessionEvent>>? writers))
            {
                return;
            }

            writers.Remove(writer);
            if (writers.Count == 0)
            {
                _subscribers.Remove(sessionId);
            }
        }
    }

    private static DomainAiSessionEvent ToEntity(AiSessionEvent evt)
    {
        return new DomainAiSessionEvent
        {
            Id = evt.EventId == Guid.Empty ? Guid.NewGuid() : evt.EventId,
            AiSessionId = evt.SessionId,
            Sequence = evt.Sequence,
            Type = evt.Type,
            OccurredAtUtc = evt.OccurredAtUtc,
            Status = evt.Status,
            Message = evt.Message,
            Delta = evt.Delta,
            IsTerminal = evt.IsTerminal,
        };
    }

    private static AiSessionEvent ToApplicationEvent(DomainAiSessionEvent evt)
    {
        return new AiSessionEvent(
            EventId: evt.Id,
            SessionId: evt.AiSessionId,
            Sequence: evt.Sequence,
            Type: evt.Type,
            OccurredAtUtc: evt.OccurredAtUtc,
            Status: evt.Status,
            Message: evt.Message,
            Delta: evt.Delta,
            IsTerminal: evt.IsTerminal);
    }

    private static string BuildTitle(string prompt)
    {
        string trimmed = prompt.Trim();
        if (trimmed.Length <= 80)
        {
            return trimmed;
        }

        return trimmed[..80];
    }
}
