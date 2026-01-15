using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.SyncState;

public sealed class GetAzureSyncStateQueryHandler : IRequestHandler<GetAzureSyncStateQuery, AzureSyncState?>
{
    private readonly IAzureConnectionRepository _connections;
    private readonly IAzureSyncStateRepository _syncStates;

    public GetAzureSyncStateQueryHandler(IAzureConnectionRepository connections, IAzureSyncStateRepository syncStates)
    {
        _connections = connections;
        _syncStates = syncStates;
    }

    public async Task<AzureSyncState?> Handle(GetAzureSyncStateQuery request, CancellationToken cancellationToken)
    {
        var connection = await _connections.GetSingletonAsync(cancellationToken);
        if (connection is null) return null;

        return await _syncStates.GetByConnectionIdAsync(connection.Id, cancellationToken);
    }
}
