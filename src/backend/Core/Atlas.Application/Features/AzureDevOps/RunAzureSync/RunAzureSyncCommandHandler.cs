using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Abstractions.Time;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using System.Globalization;

namespace Atlas.Application.Features.AzureDevOps.RunAzureSync;

public sealed class RunAzureSyncCommandHandler : IRequestHandler<RunAzureSyncCommand, RunAzureSyncResult>
{
    private const int PageSize = 200;

    private readonly IAzureDevOpsClient _client;
    private readonly IAzureConnectionRepository _connections;
    private readonly IAzureSyncStateRepository _syncStates;
    private readonly IAzureWorkItemRepository _workItems;
    private readonly ISettingsRepository _settings;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public RunAzureSyncCommandHandler(
        IAzureDevOpsClient client,
        IAzureConnectionRepository connections,
        IAzureSyncStateRepository syncStates,
        IAzureWorkItemRepository workItems,
        ISettingsRepository settings,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _client = client;
        _connections = connections;
        _syncStates = syncStates;
        _workItems = workItems;
        _settings = settings;
        _uow = uow;
        _clock = clock;
    }

    public async Task<RunAzureSyncResult> Handle(RunAzureSyncCommand request, CancellationToken cancellationToken)
    {
        var connection = await _connections.GetSingletonAsync(cancellationToken);
        if (connection is null || !connection.IsEnabled)
        {
            return new RunAzureSyncResult(true, 0, 0, null, null, "Azure DevOps connection is not configured or disabled.");
        }

        var settings = await _settings.GetSingletonAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(settings?.AzureDevOpsBaseUrl)
            ? "https://dev.azure.com"
            : settings!.AzureDevOpsBaseUrl!.Trim();

        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var state = await _syncStates.GetByConnectionIdAsync(connection.Id, cancellationToken);
        if (state is null)
        {
            state = new AzureSyncState
            {
                Id = Guid.NewGuid(),
                AzureConnectionId = connection.Id,
                LastRunStatus = SyncRunStatus.NeverRun
            };
            await _syncStates.AddAsync(state, cancellationToken);
        }

        var startedAt = _clock.UtcNow;
        state.LastAttemptedAtUtc = startedAt;
        state.LastRunStatus = SyncRunStatus.Running;
        state.LastError = null;
        await _uow.SaveChangesAsync(cancellationToken);

        var currentChanged = state.LastSuccessfulChangedUtc;
        var currentId = state.LastSuccessfulWorkItemId;
        var totalFetched = 0;
        var totalUpserted = 0;

        try
        {
            while (true)
            {
                var wiql = BuildWiql(connection, currentChanged, currentId);
                var ids = await _client.QueryWorkItemIdsAsync(
                    baseUrl,
                    connection.Organization,
                    connection.Project,
                    wiql,
                    PageSize,
                    cancellationToken);
                if (ids.Count == 0) break;

                totalFetched += ids.Count;
                var details = await _client.GetWorkItemsAsync(baseUrl, connection.Organization, connection.Project, ids, cancellationToken);

                var existing = await _workItems.GetByWorkItemIdsAsync(connection.Id, ids, cancellationToken);
                var existingById = existing.ToDictionary(x => x.WorkItemId);

                foreach (var item in details)
                {
                    if (!existingById.TryGetValue(item.Id, out var entity))
                    {
                        entity = new AzureWorkItem
                        {
                            Id = Guid.NewGuid(),
                            AzureConnectionId = connection.Id,
                            WorkItemId = item.Id
                        };
                        await _workItems.AddAsync(entity, cancellationToken);
                    }

                    entity.Rev = item.Rev;
                    entity.ChangedDateUtc = item.ChangedDateUtc;
                    entity.Title = item.Title;
                    entity.State = item.State;
                    entity.WorkItemType = item.WorkItemType;
                    entity.AreaPath = item.AreaPath;
                    entity.IterationPath = item.IterationPath;
                    entity.AssignedToUniqueName = NormalizeUniqueName(item.AssignedToUniqueName);
                    entity.Url = item.Url;

                    totalUpserted++;

                    if (ShouldAdvanceWatermark(currentChanged, currentId, item.ChangedDateUtc, item.Id))
                    {
                        currentChanged = item.ChangedDateUtc;
                        currentId = item.Id;
                    }
                }

                await _uow.SaveChangesAsync(cancellationToken);

                if (ids.Count < PageSize) break;
            }

            state.LastSuccessfulChangedUtc = currentChanged;
            state.LastSuccessfulWorkItemId = currentId;
            state.LastRunStatus = SyncRunStatus.Succeeded;
            state.LastCompletedAtUtc = _clock.UtcNow;
            state.LastError = null;

            await _uow.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new RunAzureSyncResult(true, totalFetched, totalUpserted, currentChanged, currentId, null);
        }
        catch (Exception ex)
        {
            state.LastRunStatus = SyncRunStatus.Failed;
            state.LastCompletedAtUtc = _clock.UtcNow;
            state.LastError = ex.Message;
            await tx.RollbackAsync(cancellationToken);

            return new RunAzureSyncResult(false, totalFetched, totalUpserted, currentChanged, currentId, ex.Message);
        }
    }

    private static bool ShouldAdvanceWatermark(DateTimeOffset? currentChanged, int? currentId, DateTimeOffset nextChanged, int nextId)
    {
        if (currentChanged is null) return true;
        if (nextChanged > currentChanged) return true;
        return nextChanged == currentChanged && (!currentId.HasValue || nextId > currentId.Value);
    }

    private static string BuildWiql(AzureConnection connection, DateTimeOffset? lastChangedUtc, int? lastWorkItemId)
    {
        var clauses = new List<string>
        {
            $"[System.TeamProject] = @project",
            $"[System.AreaPath] UNDER '{EscapeWiql(connection.AreaPath)}'"
        };

        if (lastChangedUtc.HasValue)
        {
            var changed = lastChangedUtc.Value.ToString("o", CultureInfo.InvariantCulture);
            if (lastWorkItemId.HasValue)
            {
                clauses.Add($"(([System.ChangedDate] > '{changed}') OR ([System.ChangedDate] = '{changed}' AND [System.Id] > {lastWorkItemId.Value}))");
            }
            else
            {
                clauses.Add($"[System.ChangedDate] >= '{changed}'");
            }
        }

        var where = string.Join(" AND ", clauses);
        return $"SELECT [System.Id] FROM WorkItems WHERE {where} ORDER BY [System.ChangedDate] ASC, [System.Id] ASC";
    }

    private static string EscapeWiql(string value) => value.Replace("'", "''", StringComparison.Ordinal).Replace("\\", "\\\\");

    private static string? NormalizeUniqueName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToLowerInvariant();
    }
}
