using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.ListConversations;

public sealed record ListAiConversationsQuery(int Take = 25) : IRequest<IReadOnlyList<AiConversation>>;
