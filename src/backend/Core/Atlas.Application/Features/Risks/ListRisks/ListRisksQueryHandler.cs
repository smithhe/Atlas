using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Risks.ListRisks;

public sealed class ListRisksQueryHandler : IRequestHandler<ListRisksQuery, IReadOnlyList<Risk>>
{
    private readonly IRiskRepository _risks;

    public ListRisksQueryHandler(IRiskRepository risks)
    {
        _risks = risks;
    }

    public Task<IReadOnlyList<Risk>> Handle(ListRisksQuery request, CancellationToken cancellationToken)
    {
        return _risks.ListAsync(cancellationToken);
    }
}

