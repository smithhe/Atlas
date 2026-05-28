namespace Atlas.Application.Abstractions.Ai;

public sealed record AiSessionStartRequest(
    Guid ConversationId,
    int TurnIndex,
    string Prompt,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId);

