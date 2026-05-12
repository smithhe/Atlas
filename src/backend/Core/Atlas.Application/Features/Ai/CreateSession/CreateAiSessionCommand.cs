using Atlas.Application.Abstractions.Ai;

namespace Atlas.Application.Features.Ai.CreateSession;

public sealed record CreateAiSessionCommand(
    string Prompt,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId) : IRequest<Guid>;

