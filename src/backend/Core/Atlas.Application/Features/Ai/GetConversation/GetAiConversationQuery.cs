using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.GetConversation;

public sealed record GetAiConversationQuery(Guid ConversationId) : IRequest<AiConversation?>;
