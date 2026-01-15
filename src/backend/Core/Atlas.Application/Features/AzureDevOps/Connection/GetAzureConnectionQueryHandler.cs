using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.Connection;

public sealed class GetAzureConnectionQueryHandler : IRequestHandler<GetAzureConnectionQuery, AzureConnection?>
{
    private readonly IAzureConnectionRepository _connections;

    public GetAzureConnectionQueryHandler(IAzureConnectionRepository connections)
    {
        _connections = connections;
    }

    public Task<AzureConnection?> Handle(GetAzureConnectionQuery request, CancellationToken cancellationToken)
    {
        return _connections.GetSingletonAsync(cancellationToken);
    }
}
