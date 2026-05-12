using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.ListSessions;

public sealed class ListAiSessionsQueryHandler : IRequestHandler<ListAiSessionsQuery, IReadOnlyList<AiSession>>
{
    private readonly IAiSessionRepository _sessions;

    public ListAiSessionsQueryHandler(IAiSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public Task<IReadOnlyList<AiSession>> Handle(ListAiSessionsQuery request, CancellationToken cancellationToken)
    {
        return _sessions.ListRecentAsync(request.Take, cancellationToken);
    }
}
