using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.GetSession;

public sealed class GetAiSessionQueryHandler : IRequestHandler<GetAiSessionQuery, AiSession?>
{
    private readonly IAiSessionRepository _sessions;

    public GetAiSessionQueryHandler(IAiSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public Task<AiSession?> Handle(GetAiSessionQuery request, CancellationToken cancellationToken)
    {
        return _sessions.GetByIdWithEventsAsync(request.SessionId, cancellationToken);
    }
}
