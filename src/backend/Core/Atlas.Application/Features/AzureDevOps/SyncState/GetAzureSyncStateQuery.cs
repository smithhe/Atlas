using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.SyncState;

public sealed record GetAzureSyncStateQuery : IRequest<AzureSyncState?>;
