using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;

namespace Atlas.Domain.Entities;

public sealed class AzureSyncState : AggregateRoot
{
    public Guid AzureConnectionId { get; set; }
    public AzureConnection? AzureConnection { get; set; }

    public DateTimeOffset? LastSuccessfulChangedUtc { get; set; }
    public int? LastSuccessfulWorkItemId { get; set; }

    public DateTimeOffset? LastAttemptedAtUtc { get; set; }
    public DateTimeOffset? LastCompletedAtUtc { get; set; }
    public SyncRunStatus LastRunStatus { get; set; } = SyncRunStatus.NeverRun;
    public string? LastError { get; set; }
}
