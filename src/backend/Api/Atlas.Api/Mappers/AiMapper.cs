using Atlas.Api.DTOs.Ai;
using Atlas.Application.Abstractions.Ai;
using DomainAiSession = Atlas.Domain.Entities.AiSession;
using DomainAiSessionEvent = Atlas.Domain.Entities.AiSessionEvent;

namespace Atlas.Api.Mappers;

public static class AiMapper
{
    public static AiSessionListItemDto ToListItemDto(DomainAiSession session)
    {
        return new AiSessionListItemDto(
            SessionId: session.Id,
            Title: session.Title,
            Prompt: session.Prompt,
            View: ParseView(session.View),
            ActionId: session.ActionId,
            TaskId: session.TaskId,
            ProjectId: session.ProjectId,
            RiskId: session.RiskId,
            TeamMemberId: session.TeamMemberId,
            CreatedAtUtc: session.CreatedAtUtc,
            CompletedAtUtc: session.CompletedAtUtc,
            Status: session.Status,
            IsTerminal: session.IsTerminal);
    }

    public static AiSessionDetailDto ToDetailDto(DomainAiSession session)
    {
        return new AiSessionDetailDto(
            SessionId: session.Id,
            Title: session.Title,
            Prompt: session.Prompt,
            View: ParseView(session.View),
            ActionId: session.ActionId,
            TaskId: session.TaskId,
            ProjectId: session.ProjectId,
            RiskId: session.RiskId,
            TeamMemberId: session.TeamMemberId,
            CreatedAtUtc: session.CreatedAtUtc,
            CompletedAtUtc: session.CompletedAtUtc,
            Status: session.Status,
            IsTerminal: session.IsTerminal,
            Events: session.Events.OrderBy(x => x.Sequence).Select(ToEventDto).ToList());
    }

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

    private static AiViewScope ParseView(string view)
    {
        return Enum.TryParse(view, ignoreCase: true, out AiViewScope parsed)
            ? parsed
            : AiViewScope.Dashboard;
    }
}
