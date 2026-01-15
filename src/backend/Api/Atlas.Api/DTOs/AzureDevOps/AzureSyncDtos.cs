namespace Atlas.Api.DTOs.AzureDevOps;

public sealed record AzureSyncResultDto(
    bool Succeeded,
    int ItemsFetched,
    int ItemsUpserted,
    DateTimeOffset? LastChangedUtc,
    int? LastWorkItemId,
    string? Error);

public sealed record AzureSyncStateDto(
    DateTimeOffset? LastSuccessfulChangedUtc,
    int? LastSuccessfulWorkItemId,
    DateTimeOffset? LastAttemptedAtUtc,
    DateTimeOffset? LastCompletedAtUtc,
    string LastRunStatus,
    string? LastError);
