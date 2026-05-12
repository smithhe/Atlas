using Atlas.Application.Abstractions.Ai;

namespace Atlas.Api.DTOs.Ai;

public sealed record CreateAiSessionRequest(
    string Prompt,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId);

public sealed record CreateAiSessionResponse(Guid SessionId);

