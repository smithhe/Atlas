using Atlas.Api.DTOs.Ai;
using DomainAiSessionEvent = Atlas.Domain.Entities.AiSessionEvent;

namespace Atlas.Api.Mappers;

public static class AiMapper
{
    public static AiSessionEventDto ToEventDto(DomainAiSessionEvent evt)
    {
        return new AiSessionEventDto(
            EventId: evt.Id,
            SessionId: evt.AiSessionId,
            Sequence: evt.Sequence,
            Type: evt.Type,
            Status: evt.Status,
            Message: evt.Message,
            Delta: evt.Delta,
            OccurredAtUtc: evt.OccurredAtUtc,
            IsTerminal: evt.IsTerminal);
    }
}
