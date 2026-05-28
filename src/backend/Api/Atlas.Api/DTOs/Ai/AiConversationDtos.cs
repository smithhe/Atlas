using Atlas.Application.Abstractions.Ai;

namespace Atlas.Api.DTOs.Ai;

public sealed record CreateAiConversationRequest(
    string Prompt,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId);

public sealed record CreateAiConversationResponse(Guid ConversationId, Guid TurnSessionId);

public sealed record ContinueAiConversationRequest
{
    public string Prompt { get; set; } = string.Empty;
}

public sealed record ContinueAiConversationResponse(Guid TurnSessionId);

public sealed class ListAiConversationsRequest
{
    [QueryParam]
    public int? Take { get; set; }
}

public sealed class GetAiConversationRequest
{
    public Guid ConversationId { get; set; }
}

public sealed record AiConversationListItemDto(
    Guid ConversationId,
    string Title,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int TurnCount,
    string Status,
    bool IsTerminal);

public sealed record AiConversationTurnDto(
    Guid SessionId,
    int TurnIndex,
    string Prompt,
    string Status,
    bool IsTerminal,
    IReadOnlyList<AiSessionEventDto> Events);

public sealed record AiConversationDetailDto(
    Guid ConversationId,
    string Title,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<AiConversationTurnDto> Turns);
