using Atlas.Api.DTOs.Ai;
using Atlas.Application.Abstractions.Ai;
using Atlas.Domain.Entities;

namespace Atlas.Api.Mappers;

public static class AiConversationMapper
{
    public static AiConversationListItemDto ToListItemDto(AiConversation conversation)
    {
        AiSession? latestTurn = conversation.Turns
            .OrderByDescending(t => t.TurnIndex)
            .FirstOrDefault();

        return new AiConversationListItemDto(
            ConversationId: conversation.Id,
            Title: conversation.Title,
            View: ParseView(conversation.View),
            ActionId: conversation.ActionId,
            TaskId: conversation.TaskId,
            ProjectId: conversation.ProjectId,
            RiskId: conversation.RiskId,
            TeamMemberId: conversation.TeamMemberId,
            CreatedAtUtc: conversation.CreatedAtUtc,
            UpdatedAtUtc: conversation.UpdatedAtUtc,
            TurnCount: conversation.Turns.Count,
            Status: latestTurn?.Status ?? "created",
            IsTerminal: latestTurn?.IsTerminal ?? false);
    }

    public static AiConversationDetailDto ToDetailDto(AiConversation conversation)
    {
        return new AiConversationDetailDto(
            ConversationId: conversation.Id,
            Title: conversation.Title,
            View: ParseView(conversation.View),
            ActionId: conversation.ActionId,
            TaskId: conversation.TaskId,
            ProjectId: conversation.ProjectId,
            RiskId: conversation.RiskId,
            TeamMemberId: conversation.TeamMemberId,
            CreatedAtUtc: conversation.CreatedAtUtc,
            UpdatedAtUtc: conversation.UpdatedAtUtc,
            Turns: conversation.Turns
                .OrderBy(t => t.TurnIndex)
                .Select(ToTurnDto)
                .ToList());
    }

    private static AiConversationTurnDto ToTurnDto(AiSession turn)
    {
        return new AiConversationTurnDto(
            SessionId: turn.Id,
            TurnIndex: turn.TurnIndex,
            Prompt: turn.Prompt,
            Status: turn.Status,
            IsTerminal: turn.IsTerminal,
            Events: turn.Events.OrderBy(e => e.Sequence).Select(AiMapper.ToEventDto).ToList());
    }

    private static AiViewScope ParseView(string view)
    {
        if (Enum.TryParse(view, ignoreCase: true, out AiViewScope parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        // Unknown / legacy values only — known scopes (Team, Risks, Projects, Settings, …) parse above.
        return AiViewScope.Dashboard;
    }
}
