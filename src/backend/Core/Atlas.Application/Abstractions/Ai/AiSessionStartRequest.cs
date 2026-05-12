namespace Atlas.Application.Abstractions.Ai;

public sealed record AiSessionStartRequest(
    string Prompt,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId);

