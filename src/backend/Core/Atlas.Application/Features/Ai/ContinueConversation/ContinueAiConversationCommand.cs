namespace Atlas.Application.Features.Ai.ContinueConversation;

public sealed record ContinueAiConversationCommand(Guid ConversationId, string Prompt) : IRequest<ContinueAiConversationResult>;

public sealed record ContinueAiConversationResult(Guid TurnSessionId);
