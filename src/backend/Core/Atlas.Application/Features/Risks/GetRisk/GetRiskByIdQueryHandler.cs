using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Risks.GetRisk;

public sealed class GetRiskByIdQueryHandler : IRequestHandler<GetRiskByIdQuery, Risk?>
{
    private readonly IRiskRepository _risks;

    public GetRiskByIdQueryHandler(IRiskRepository risks)
    {
        _risks = risks;
    }

    public Task<Risk?> Handle(GetRiskByIdQuery request, CancellationToken cancellationToken)
    {
        return request.IncludeDetails
            ? _risks.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            : _risks.GetByIdAsync(request.Id, cancellationToken);
    }
}

