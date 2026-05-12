using Atlas.Application.Abstractions.Ai;

namespace Atlas.Api.DTOs.Ai;

public sealed class ListAiSessionsRequest
{
    [QueryParam]
    public int? Take { get; set; }
}

public sealed class GetAiSessionRequest
{
    public Guid SessionId { get; set; }
}

public sealed record AiSessionListItemDto(
    Guid SessionId,
    string Title,
    string Prompt,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string Status,
    bool IsTerminal);

public sealed record AiSessionDetailDto(
    Guid SessionId,
    string Title,
    string Prompt,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string Status,
    bool IsTerminal,
    IReadOnlyList<AiSessionEventDto> Events);
