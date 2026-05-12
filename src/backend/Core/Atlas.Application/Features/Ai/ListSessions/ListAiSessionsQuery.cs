using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.ListSessions;

public sealed record ListAiSessionsQuery(int Take = 25) : IRequest<IReadOnlyList<AiSession>>;
