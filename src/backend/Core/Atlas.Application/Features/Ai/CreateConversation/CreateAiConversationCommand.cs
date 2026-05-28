using Atlas.Application.Abstractions.Ai;

namespace Atlas.Application.Features.Ai.CreateConversation;

public sealed record CreateAiConversationCommand(
    string Prompt,
    AiViewScope View,
    string? ActionId,
    Guid? TaskId,
    Guid? ProjectId,
    Guid? RiskId,
    Guid? TeamMemberId) : IRequest<CreateAiConversationResult>;

public sealed record CreateAiConversationResult(Guid ConversationId, Guid TurnSessionId);
