namespace Atlas.Application.Features.AzureDevOps.RunAzureSync;

public sealed record RunAzureSyncResult(
    bool Succeeded,
    int ItemsFetched,
    int ItemsUpserted,
    DateTimeOffset? LastChangedUtc,
    int? LastWorkItemId,
    string? Error);
