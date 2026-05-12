using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.GetSession;

public sealed record GetAiSessionQuery(Guid SessionId) : IRequest<AiSession?>;
